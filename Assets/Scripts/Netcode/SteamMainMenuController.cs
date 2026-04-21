using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SteamMainMenuController : MonoBehaviour
{
    [Header("Scene UI References (optional)")]
    [SerializeField] private Canvas sceneCanvas;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button inviteFriendsButton;
    [SerializeField] private Button startButton;

    private SteamLobbyManager _lobbyManager;

    private GameObject _mainPanel;
    private GameObject _lobbyPanel;
    private TMP_Text _statusText;
    private TMP_Text _playersText;
    private Button _startButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateForMainMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.name != "MainMenu")
            return;

        if (FindFirstObjectByType<SteamMainMenuController>() != null)
            return;

        GameObject root = new GameObject("SteamMainMenuController");
        root.AddComponent<SteamMainMenuController>();
    }

    private void Awake()
    {
        EnsureSteamObjects();
        EnsureEventSystem();
        BuildOrBindMenuUI();
        WireLobbyEvents();
        SwitchToMainPanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (_lobbyManager == null)
            return;

        _lobbyManager.StatusChangedEvent -= HandleStatusChanged;
        _lobbyManager.LobbyEnteredEvent -= HandleLobbyEntered;
        _lobbyManager.LobbyLeftEvent -= HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent -= HandleMembersChanged;
    }

    private void EnsureSteamObjects()
    {
        if (FindFirstObjectByType<SteamManager>() == null)
        {
            GameObject steamManagerGo = new GameObject("SteamManager");
            steamManagerGo.AddComponent<SteamManager>();
        }

        if (FindFirstObjectByType<MusicDirector>() == null)
        {
            GameObject musicDirectorGo = new GameObject("MusicDirector");
            musicDirectorGo.AddComponent<MusicDirector>();
        }

        _lobbyManager = FindFirstObjectByType<SteamLobbyManager>();
        if (_lobbyManager == null)
        {
            GameObject lobbyManagerGo = new GameObject("SteamLobbyManager");
            _lobbyManager = lobbyManagerGo.AddComponent<SteamLobbyManager>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private void WireLobbyEvents()
    {
        _lobbyManager.StatusChangedEvent += HandleStatusChanged;
        _lobbyManager.LobbyEnteredEvent += HandleLobbyEntered;
        _lobbyManager.LobbyLeftEvent += HandleLobbyLeft;
        _lobbyManager.LobbyMembersChangedEvent += HandleMembersChanged;
    }

    private void BuildOrBindMenuUI()
    {
        bool hasSceneUi =
            mainPanel != null &&
            lobbyPanel != null &&
            statusText != null &&
            playersText != null &&
            createLobbyButton != null &&
            joinLobbyButton != null &&
            quitButton != null &&
            leaveLobbyButton != null &&
            inviteFriendsButton != null &&
            startButton != null;

        if (hasSceneUi)
        {
            _mainPanel = mainPanel;
            _lobbyPanel = lobbyPanel;
            _statusText = statusText;
            _playersText = playersText;
            _startButton = startButton;

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
            _startButton.interactable = false;

            if (string.IsNullOrEmpty(_statusText.text))
                _statusText.text = "Готово";
            if (string.IsNullOrEmpty(_playersText.text))
                _playersText.text = "Игроки в лобби:\n-";
            return;
        }

        BuildMenuUIRuntime();
    }

    private void BuildMenuUIRuntime()
    {
        Canvas canvas = sceneCanvas != null ? sceneCanvas : CreateRootCanvas();
        _mainPanel = CreatePanel(canvas.transform, "MainPanel");
        _lobbyPanel = CreatePanel(canvas.transform, "LobbyPanel");

        CreateButton(_mainPanel.transform, "Создать лобби", new Vector2(0f, 120f), () => _lobbyManager.HostGame());
        CreateButton(_mainPanel.transform, "Присоединиться к лобби", new Vector2(0f, 20f), () => _lobbyManager.OpenFriendsOverlay());
        CreateButton(_mainPanel.transform, "Выйти", new Vector2(0f, -80f), Application.Quit);

        CreateHeader(_mainPanel.transform, "MAIN MENU", new Vector2(0f, 260f));
        _statusText = CreateText(_mainPanel.transform, "StatusText", new Vector2(0f, -210f), 30, TextAlignmentOptions.Center);
        _statusText.text = "Готово";

        CreateHeader(_lobbyPanel.transform, "LOBBY", new Vector2(0f, 260f));
        _playersText = CreateText(_lobbyPanel.transform, "PlayersText", new Vector2(0f, 50f), 28, TextAlignmentOptions.TopLeft, new Vector2(700f, 320f));
        _playersText.text = "Игроки в лобби:\n-";

        CreateButton(_lobbyPanel.transform, "Выйти в меню", new Vector2(-220f, -220f), () => _lobbyManager.LeaveLobby(), new Vector2(220f, 62f), 22);
        CreateButton(_lobbyPanel.transform, "Пригласить друзей", new Vector2(0f, -220f), () => _lobbyManager.InviteFriend(), new Vector2(220f, 62f), 22);
        _startButton = CreateButton(
            _lobbyPanel.transform,
            "Запуск",
            new Vector2(220f, -220f),
            () =>
            {
                MusicDirector.StopGlobal();
                _lobbyManager.StartGameForLobby("NetworkTest");
            },
            new Vector2(220f, 62f),
            22);
        _startButton.interactable = false;
    }

    private static Canvas CreateRootCanvas()
    {
        GameObject go = new GameObject("MainMenuCanvas");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.05f, 0.07f, 0.1f, 0.92f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static void CreateHeader(Transform parent, string text, Vector2 anchoredPosition)
    {
        TMP_Text header = CreateText(parent, "Header_" + text, anchoredPosition, 52, TextAlignmentOptions.Center);
        header.text = text;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchoredPosition, int size, TextAlignmentOptions alignment, Vector2? rectSize = null)
    {
        GameObject textGo = new GameObject(name);
        textGo.transform.SetParent(parent, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = string.Empty;

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = rectSize ?? new Vector2(1100f, 110f);
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string label,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick,
        Vector2? size = null,
        int fontSize = 30)
    {
        GameObject buttonGo = new GameObject("Button_" + label);
        buttonGo.transform.SetParent(parent, false);
        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.17f, 0.26f, 0.39f, 1f);
        Button button = buttonGo.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = image.color;
        cb.highlightedColor = new Color(0.25f, 0.35f, 0.5f, 1f);
        cb.pressedColor = new Color(0.1f, 0.2f, 0.32f, 1f);
        button.colors = cb;
        button.onClick.AddListener(onClick);

        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        Vector2 finalSize = size ?? new Vector2(360f, 78f);
        rt.sizeDelta = finalSize;

        TMP_Text text = CreateText(buttonGo.transform, "Label", Vector2.zero, fontSize, TextAlignmentOptions.Center, finalSize - new Vector2(20f, 8f));
        text.text = label;
        return button;
    }

    private void HandleLobbyEntered(Steamworks.CSteamID _)
    {
        SwitchToLobbyPanel();
        HandleMembersChanged(_lobbyManager.GetLobbyMemberNames());
        UpdateLobbyButtons();
        MusicDirector.PlayGlobal(MusicCue.Lobby);
    }

    private void HandleLobbyLeft()
    {
        SwitchToMainPanel();
        MusicDirector.StopGlobal();
    }

    private void HandleStatusChanged(string status)
    {
        if (_statusText != null)
            _statusText.text = status;
    }

    private void HandleMembersChanged(IReadOnlyList<string> members)
    {
        if (_playersText == null)
            return;

        if (members == null || members.Count == 0)
        {
            _playersText.text = "Игроки в лобби:\n-";
            return;
        }

        List<string> lines = new List<string>(members.Count + 1) { "Игроки в лобби:" };
        for (int i = 0; i < members.Count; i++)
            lines.Add($"- {members[i]}");

        _playersText.text = string.Join("\n", lines);
        UpdateLobbyButtons();
    }

    private void SwitchToMainPanel()
    {
        _mainPanel.SetActive(true);
        _lobbyPanel.SetActive(false);
    }

    private void SwitchToLobbyPanel()
    {
        _mainPanel.SetActive(false);
        _lobbyPanel.SetActive(true);
        UpdateLobbyButtons();
    }

    private void UpdateLobbyButtons()
    {
        if (_startButton == null || _lobbyManager == null)
            return;

        bool isHost = _lobbyManager.IsLobbyHost;
        _startButton.interactable = isHost;
        TMP_Text label = _startButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = isHost ? "Запуск" : "Ожидание хоста";
    }
}
