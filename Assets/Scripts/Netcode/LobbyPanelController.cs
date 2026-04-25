using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanelController : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Player Slots (4 roots)")]
    [SerializeField] private Transform[] slotRoots = new Transform[4];
    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private GameObject lobbyItemEmptyPrefab;

    [Header("Buttons")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button inviteFriendsButton;
    [SerializeField] private Button startButton;

    private SteamLobbyManager _lobbyManager;
    private readonly GameObject[] _slotInstances = new GameObject[4];


    private void Awake()
    {
        _lobbyManager = FindFirstObjectByType<SteamLobbyManager>();
        if (_lobbyManager == null)
        {
            Debug.LogError("[LobbyPanel] SteamLobbyManager not found in scene.");
            return;
        }

        BindButtons();
        WireLobbyEvents();
    }

    private void OnDestroy()
    {
        if (_lobbyManager == null) return;
        _lobbyManager.StatusChangedEvent -= HandleStatusChanged;
        _lobbyManager.LobbyLeftEvent -= HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent -= HandleMembersChanged;
        _lobbyManager.LobbyReadyChangedEvent -= HandleReadyChanged;
    }

    private void BindButtons()
    {
        leaveLobbyButton.onClick.RemoveAllListeners();
        inviteFriendsButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();

        leaveLobbyButton.onClick.AddListener(() => _lobbyManager.LeaveLobby());
        inviteFriendsButton.onClick.AddListener(() => _lobbyManager.InviteFriend());
        startButton.onClick.AddListener(OnStartClicked);

        startButton.interactable = false;
    }

    private void WireLobbyEvents()
    {
        _lobbyManager.StatusChangedEvent += HandleStatusChanged;
        _lobbyManager.LobbyLeftEvent += HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent += HandleMembersChanged;
        _lobbyManager.LobbyReadyChangedEvent += HandleReadyChanged;
    }
    
    private void HandleStatusChanged(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    private void HandleLobbyLeft()
    {
        ClearAllSlots();
    }

    private void HandleMembersChanged(IReadOnlyList<string> _)
    {
        RefreshAllSlots();
        UpdateStartButton();
    }

    private void HandleReadyChanged()
    {
        for (int i = 0; i < _slotInstances.Length; i++)
            _slotInstances[i]?.GetComponent<LobbyItemUI>()?.Refresh();

        UpdateStartButton();
    }

    private void OnStartClicked()
    {
        MusicDirector.StopGlobal();
        _lobbyManager.StartGameForLobby("MainScenes_LVL1");
    }

    private void RefreshAllSlots()
    {
        List<CSteamID> members = _lobbyManager.GetLobbyMemberIDs();

        for (int i = 0; i < slotRoots.Length; i++)
        {
            bool hasPlayer = members != null && i < members.Count;

            if (hasPlayer)
            {
                CSteamID id = members[i];

                // If current instance is wrong type, destroy it
                if (_slotInstances[i] != null && _slotInstances[i].GetComponent<LobbyItemUI>() == null)
                {
                    Destroy(_slotInstances[i]);
                    _slotInstances[i] = null;
                }

                if (_slotInstances[i] == null)
                {
                    _slotInstances[i] = Instantiate(lobbyItemPrefab, slotRoots[i]);
                    ResetRectTransform(_slotInstances[i]);
                }

                _slotInstances[i].SetActive(true);
                _slotInstances[i].GetComponent<LobbyItemUI>().Init(id, _lobbyManager);
            }
            else
            {
                // If current instance is a player slot, destroy it
                if (_slotInstances[i] != null && _slotInstances[i].GetComponent<LobbyItemUI>() != null)
                {
                    Destroy(_slotInstances[i]);
                    _slotInstances[i] = null;
                }

                if (_slotInstances[i] == null)
                {
                    _slotInstances[i] = Instantiate(lobbyItemEmptyPrefab, slotRoots[i]);
                    ResetRectTransform(_slotInstances[i]);
                }

                _slotInstances[i].SetActive(true);
            }
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < _slotInstances.Length; i++)
        {
            if (_slotInstances[i] != null)
            {
                Destroy(_slotInstances[i]);
                _slotInstances[i] = null;
            }
        }
    }

    private static void ResetRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.localPosition = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
    }
    
    private void UpdateStartButton()
    {
        if (startButton == null || _lobbyManager == null) return;

        bool isHost= _lobbyManager.IsLobbyHost;
        bool allReady = _lobbyManager.AreAllPlayersReady();

        startButton.interactable = isHost && allReady;

        TMP_Text label = startButton.GetComponentInChildren<TMP_Text>();
        if (label == null) return;

        if (!isHost)
            label.text = "Ожидание хоста";
        else if (!allReady)
            label.text = "Ожидание готовности";
        else
            label.text = "Запуск";
    }
    
}