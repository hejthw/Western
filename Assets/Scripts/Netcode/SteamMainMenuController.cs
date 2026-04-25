using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SteamMainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Panel Buttons")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Lobby Buttons")]
    [SerializeField] private Button backToMainButton;

    [Header("Settings buttons")]
    [SerializeField] private Button backToMainMenuButton;
    private SteamLobbyManager _lobbyManager;
    
    private void Awake()
    {
        EnsureEventSystem();
        EnsureDependencies();
        BindButtons();
        WireLobbyEvents();
        SwitchToMain();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (_lobbyManager == null) return;
        _lobbyManager.LobbyEnteredEvent -= OnLobbyEntered;
        _lobbyManager.LobbyLeftEvent -= OnLobbyLeft;
    }
    

    private void EnsureDependencies()
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
        settingsButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        backToMainButton.onClick.RemoveAllListeners();


        createLobbyButton.onClick.AddListener(() => _lobbyManager.HostGame());
        joinLobbyButton.onClick.AddListener(() => _lobbyManager.OpenFriendsOverlay());
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(Application.Quit);
        backToMainButton.onClick.AddListener(CloseSettings);
        backToMainMenuButton.onClick.AddListener(CloseSettings);
    }

    private void WireLobbyEvents()
    {
        _lobbyManager.LobbyEnteredEvent += OnLobbyEntered;
        _lobbyManager.LobbyLeftEvent += OnLobbyLeft;
    }
    

    private void OnLobbyEntered(Steamworks.CSteamID _) => SwitchToLobby();
    private void OnLobbyLeft() => SwitchToMain();
    

    public void SwitchToMain()
    {
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void SwitchToLobby()
    {
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings() => SwitchToMain();
}