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
    [SerializeField] private Button   createLobbyButton;
    [SerializeField] private Button   joinLobbyButton;
    [SerializeField] private Button   quitButton;

    [Header("Lobby Panel — Slots")]
    [Tooltip("Ровно 4 Transform-а — контейнеры слотов в нужном порядке")]
    [SerializeField] private Transform[] slotRoots = new Transform[4];
    [SerializeField] private GameObject  lobbyItemPrefab;
    [SerializeField] private GameObject  lobbyItemEmptyPrefab;

    [Header("Lobby Panel — Buttons")]
    [SerializeField] private Button   leaveLobbyButton;
    [SerializeField] private Button   inviteFriendsButton;
    [SerializeField] private Button   startButton;

    private SteamLobbyManager _lobbyManager;

    // Текущий тип каждого слота: true = LobbyItemUI, false = Empty, null = не заспавнен
    private readonly GameObject[] _slotInstances = new GameObject[4];

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureSteamObjects();
        EnsureEventSystem();
        BindButtons();
        WireLobbyEvents();
        SwitchToMainPanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void OnDestroy()
    {
        if (_lobbyManager == null) return;
        _lobbyManager.StatusChangedEvent       -= HandleStatusChanged;
        _lobbyManager.LobbyEnteredEvent        -= HandleLobbyEntered;
        _lobbyManager.LobbyLeftEvent           -= HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent -= HandleMembersChanged;
        _lobbyManager.LobbyReadyChangedEvent   -= HandleReadyChanged;
    }

    // ── Init ─────────────────────────────────────────────────────────────────

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
        _lobbyManager.StatusChangedEvent       += HandleStatusChanged;
        _lobbyManager.LobbyEnteredEvent        += HandleLobbyEntered;
        _lobbyManager.LobbyLeftEvent           += HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent += HandleMembersChanged;
        _lobbyManager.LobbyReadyChangedEvent   += HandleReadyChanged;
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void HandleLobbyEntered(CSteamID _)
    {
        SwitchToLobbyPanel();
        // HandleMembersChanged придёт следом от NotifyMembersChanged в менеджере
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
        if (statusText != null) statusText.text = status;
    }

    // Вызывается и при входе, и при смене состава лобби
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

    // ── Slots ─────────────────────────────────────────────────────────────────

    private void RefreshAllSlots()
    {
        List<CSteamID> members = _lobbyManager.GetLobbyMemberIDs();

        for (int i = 0; i < slotRoots.Length; i++)
        {
            bool hasPlayer = members != null && i < members.Count;

            if (hasPlayer)
            {
                CSteamID id = members[i];

                // Если на этом слоте стоит заглушка — уничтожить её
                if (_slotInstances[i] != null && _slotInstances[i].GetComponent<LobbyItemUI>() == null)
                {
                    Destroy(_slotInstances[i]);
                    _slotInstances[i] = null;
                }

                // Создать LobbyItem если нет
                if (_slotInstances[i] == null)
                {
                    _slotInstances[i] = Instantiate(lobbyItemPrefab, slotRoots[i]);
                    ResetRectTransform(_slotInstances[i]);
                    _slotInstances[i].SetActive(true);
                }

                _slotInstances[i].SetActive(true);
                // Всегда заново инициализировать — игрок мог смениться
                _slotInstances[i].GetComponent<LobbyItemUI>().Init(id, _lobbyManager);
            }
            else
            {
                // Если на этом слоте стоит LobbyItem — уничтожить его
                if (_slotInstances[i] != null && _slotInstances[i].GetComponent<LobbyItemUI>() != null)
                {
                    Destroy(_slotInstances[i]);
                    _slotInstances[i] = null;
                }

                // Создать заглушку если нет
                if (_slotInstances[i] == null)
                {
                    _slotInstances[i] = Instantiate(lobbyItemEmptyPrefab, slotRoots[i]);
                    ResetRectTransform(_slotInstances[i]);
                    _slotInstances[i].SetActive(true);
                }
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
        rt.localPosition   = Vector3.zero;
        rt.localRotation   = Quaternion.identity;
        rt.localScale      = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
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
        if (label == null) return;

        if (!isHost)          label.text = "Ожидание хоста";
        else if (!allReady)   label.text = "Ожидание готовности";
        else                  label.text = "Запуск";
    }
}