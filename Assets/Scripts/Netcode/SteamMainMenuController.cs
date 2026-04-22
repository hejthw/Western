using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class SteamMainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Main Panel")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button quitButton;

    [Header("Lobby Panel — Slots")]
    [Tooltip("Ровно 4 Transform-а — контейнеры слотов в нужном порядке")]
    [SerializeField] private Transform[] slotRoots = new Transform[4];

    [Tooltip("Префаб с компонентом LobbyItemUI")]
    [SerializeField] private GameObject lobbyItemPrefab;

    [Tooltip("Префаб с компонентом LobbySlotEmpty")]
    [SerializeField] private GameObject lobbyItemEmptyPrefab;

    [Header("Lobby Panel — Buttons")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button inviteFriendsButton;
    [SerializeField] private Button startButton;

    private SteamLobbyManager _lobbyManager;
    private readonly GameObject[] _slotInstances = new GameObject[4];

    private void Awake()
    {
        EnsureSteamObjects();
        EnsureEventSystem();
        BindButtons();
        WireLobbyEvents();
        SwitchToMainPanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (_lobbyManager == null) return;

        _lobbyManager.StatusChangedEvent -= HandleStatusChanged;
        _lobbyManager.LobbyEnteredEvent -= HandleLobbyEntered;
        _lobbyManager.LobbyLeftEvent -= HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent -= HandleMembersChanged;
        _lobbyManager.LobbyReadyChangedEvent -= HandleReadyChanged;
    }
    

    private void EnsureSteamObjects()
    {
        if (FindFirstObjectByType<SteamManager>() == null)
            new GameObject("SteamManager").AddComponent<SteamManager>();

        if (FindFirstObjectByType<MusicDirector>() == null)
            new GameObject("MusicDirector").AddComponent<MusicDirector>();

        _lobbyManager = FindFirstObjectByType<SteamLobbyManager>();
        if (_lobbyManager == null)
            _lobbyManager = new GameObject("SteamLobbyManager").AddComponent<SteamLobbyManager>();
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private void BindButtons()
    {
        createLobbyButton.onClick.RemoveAllListeners();
        joinLobbyButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        leaveLobbyButton.onClick.RemoveAllListeners();
        inviteFriendsButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();

        createLobbyButton.onClick.AddListener(() => _lobbyManager.HostGame());
        joinLobbyButton.onClick.AddListener(() => _lobbyManager.OpenFriendsOverlay());
        quitButton.onClick.AddListener(Application.Quit);
        leaveLobbyButton.onClick.AddListener(() => _lobbyManager.LeaveLobby());
        inviteFriendsButton.onClick.AddListener(() => _lobbyManager.InviteFriend());
        startButton.onClick.AddListener(() =>
        {
            MusicDirector.StopGlobal();
            _lobbyManager.StartGameForLobby("NetworkTest");
        });

        startButton.interactable = false;

        if (statusText != null && string.IsNullOrEmpty(statusText.text))
            statusText.text = "Готово";
    }

    private void WireLobbyEvents()
    {
        _lobbyManager.StatusChangedEvent += HandleStatusChanged;
        _lobbyManager.LobbyEnteredEvent += HandleLobbyEntered;
        _lobbyManager.LobbyLeftEvent += HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent += HandleMembersChanged;
        _lobbyManager.LobbyReadyChangedEvent += HandleReadyChanged;
    }
    

    private void HandleLobbyEntered(CSteamID _)
    {
        SwitchToLobbyPanel();
        RefreshAllSlots();
        UpdateStartButton();
        MusicDirector.PlayGlobal(MusicCue.Lobby);
    }

    private void HandleLobbyLeft()
    {
        ClearAllSlots();
        SwitchToMainPanel();
        MusicDirector.StopGlobal();
    }

    private void HandleStatusChanged(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    private void HandleMembersChanged(IReadOnlyList<string> _)
    {
        RefreshAllSlots();
        UpdateStartButton();
    }

    private void HandleReadyChanged()
    {
        for (int i = 0; i < _slotInstances.Length; i++)
        {
            if (_slotInstances[i] == null) continue;
            _slotInstances[i].GetComponent<LobbyItemUI>()?.Refresh();
        }
        UpdateStartButton();
    }
    

    private void RefreshAllSlots()
    {
        IReadOnlyList<CSteamID> members = _lobbyManager.GetLobbyMemberIDs();

        for (int i = 0; i < slotRoots.Length; i++)
        {
            bool hasPlayer = members != null && i < members.Count;
            CSteamID id = hasPlayer ? members[i] : default;
            
            if (_slotInstances[i] != null)
            {
                bool wasPlayer = _slotInstances[i].GetComponent<LobbyItemUI>() != null;
                if (wasPlayer != hasPlayer)
                {
                    Destroy(_slotInstances[i]);
                    _slotInstances[i] = null;
                }
            }

            if (hasPlayer)
            {
                if (_slotInstances[i] == null)
                {
                    _slotInstances[i] = Instantiate(lobbyItemPrefab, slotRoots[i]);
                    _slotInstances[i].transform.localPosition = Vector3.zero;
                }
                _slotInstances[i].GetComponent<LobbyItemUI>().Init(id, _lobbyManager);
            }
            else
            {
                if (_slotInstances[i] == null)
                {
                    _slotInstances[i] = Instantiate(lobbyItemEmptyPrefab, slotRoots[i]);
                    _slotInstances[i].transform.localPosition = Vector3.zero;
                }
            }
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < _slotInstances.Length; i++)
        {
            if (_slotInstances[i] == null) continue;
            Destroy(_slotInstances[i]);
            _slotInstances[i] = null;
        }
    }
    

    private void SwitchToMainPanel()
    {
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }

    private void SwitchToLobbyPanel()
    {
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void UpdateStartButton()
    {
        if (startButton == null || _lobbyManager == null) return;

        bool isHost   = _lobbyManager.IsLobbyHost;
        bool allReady = _lobbyManager.AreAllPlayersReady();

        startButton.interactable = isHost && allReady;

        TMP_Text label = startButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = isHost ? (allReady ? "Запуск" : "Ожидание готовности") : "Ожидание хоста";
    }
}