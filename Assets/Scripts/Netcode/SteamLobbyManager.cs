using Steamworks;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdate;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdate;

    private CSteamID m_currentLobbyID;
    private bool _isGameStartRequested;

    private const string HostAddressKey = "HostAddress";
    private const string HostNameKey = "HostName";
    private const string GameStartedKey = "GameStarted";
    private const string GameSceneKey = "GameScene";
    private const string ReadyKey = "Ready";

    public event Action<CSteamID> LobbyEnteredEvent;
    public event Action LobbyLeftEvent;
    public event Action<string> StatusChangedEvent;
    public event Action<IReadOnlyList<string>> LobbyMembersChangedEvent;
    public event Action LobbyReadyChangedEvent;

    public bool IsInLobby => m_currentLobbyID.IsValid();
    public bool IsLobbyHost => IsInLobby && SteamMatchmaking.GetLobbyOwner(m_currentLobbyID) == SteamUser.GetSteamID();
    public CSteamID CurrentLobbyId => m_currentLobbyID;

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("SteamManager not initialized!");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);

        StatusChangedEvent?.Invoke("Steam connected");
    }
    

    public void HostGame()
    {
        if (!SteamManager.Initialized) { StatusChangedEvent?.Invoke("Steam is not initialized"); return; }
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
        StatusChangedEvent?.Invoke("Creating lobby...");
    }

    public void StartGameForLobby(string sceneName)
    {
        if (!m_currentLobbyID.IsValid()) { StatusChangedEvent?.Invoke("Create or join a lobby first"); return; }
        if (!IsLobbyHost)
        { StatusChangedEvent?.Invoke("Only lobby host can start");     return; }

        _isGameStartRequested = true;
        SteamMatchmaking.SetLobbyData(m_currentLobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(m_currentLobbyID, GameSceneKey,   sceneName);
        SteamMatchmaking.SetLobbyData(m_currentLobbyID, GameStartedKey, "1");
        StatusChangedEvent?.Invoke("Starting match...");
        SteamGameStartBootstrap.SetPendingStart(true, SteamUser.GetSteamID().ToString(), sceneName);
        PrepareForGameplaySceneLoad();
        SceneManager.LoadScene(sceneName);
    }

    public void JoinGame(string lobbyIdString)
    {
        if (ulong.TryParse(lobbyIdString, out ulong id))
            SteamMatchmaking.JoinLobby(new CSteamID(id));
        else
            Debug.LogError("Invalid lobby ID.");
    }

    public void InviteFriend()
    {
        if (m_currentLobbyID.IsValid())
            SteamFriends.ActivateGameOverlayInviteDialog(m_currentLobbyID);
        else
            StatusChangedEvent?.Invoke("No active lobby");
    }

    public void OpenFriendsOverlay()
    {
        if (!SteamManager.Initialized) { StatusChangedEvent?.Invoke("Steam is not initialized"); return; }
        SteamFriends.ActivateGameOverlay("friends");
    }

    public void LeaveLobby()
    {
        if (m_currentLobbyID.IsValid())
            SteamMatchmaking.LeaveLobby(m_currentLobbyID);

        m_currentLobbyID = CSteamID.Nil;
        _isGameStartRequested = false;
        StatusChangedEvent?.Invoke("You left the lobby");
        LobbyLeftEvent?.Invoke();
        LobbyMembersChangedEvent?.Invoke(Array.Empty<string>());
    }

    public void SetLocalPlayerReady(bool ready)
    {
        if (!m_currentLobbyID.IsValid()) return;
        SteamMatchmaking.SetLobbyMemberData(m_currentLobbyID, ReadyKey, ready ? "1" : "0");
        LobbyReadyChangedEvent?.Invoke();
    }

    public bool IsPlayerReady(CSteamID playerId)
    {
        if (!m_currentLobbyID.IsValid()) return false;
        return SteamMatchmaking.GetLobbyMemberData(m_currentLobbyID, playerId, ReadyKey) == "1";
    }

    public bool AreAllPlayersReady()
    {
        if (!m_currentLobbyID.IsValid()) return false;
        int count = SteamMatchmaking.GetNumLobbyMembers(m_currentLobbyID);
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(m_currentLobbyID, i);
            if (!IsPlayerReady(member)) return false;
        }
        return true;
    }
    

    public List<string> GetLobbyMemberNames()
    {
        var names = new List<string>();
        if (!m_currentLobbyID.IsValid()) return names;
        int count = SteamMatchmaking.GetNumLobbyMembers(m_currentLobbyID);
        for (int i = 0; i < count; i++)
            names.Add(SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyMemberByIndex(m_currentLobbyID, i)));
        return names;
    }

    public List<CSteamID> GetLobbyMemberIDs()
    {
        var ids = new List<CSteamID>();
        if (!m_currentLobbyID.IsValid()) return ids;
        int count = SteamMatchmaking.GetNumLobbyMembers(m_currentLobbyID);
        for (int i = 0; i < count; i++)
            ids.Add(SteamMatchmaking.GetLobbyMemberByIndex(m_currentLobbyID, i));
        return ids;
    }
    

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult != EResult.k_EResultOK)
        {
            StatusChangedEvent?.Invoke($"Failed to create lobby: {result.m_eResult}");
            return;
        }

        m_currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        _isGameStartRequested = false;

        SteamMatchmaking.SetLobbyData(m_currentLobbyID, HostAddressKey,SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(m_currentLobbyID, HostNameKey,SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(m_currentLobbyID, GameStartedKey,"0");

        StatusChangedEvent?.Invoke("Lobby ready. Invite your friends.");
        NotifyMembersChanged();
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        m_currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        _isGameStartRequested = false;
        StatusChangedEvent?.Invoke("Joined lobby");
        
        StartCoroutine(NotifyLobbyEnteredNextFrame());
    }

    private IEnumerator NotifyLobbyEnteredNextFrame()
    {
        yield return null;
        LobbyEnteredEvent?.Invoke(m_currentLobbyID);
        NotifyMembersChanged();
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t param)
    {
        StatusChangedEvent?.Invoke("Invitation accepted. Joining lobby...");
        SteamMatchmaking.JoinLobby(param.m_steamIDLobby);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t data)
    {
        if (!m_currentLobbyID.IsValid()) return;
        if (data.m_ulSteamIDLobby != m_currentLobbyID.m_SteamID) return;
        NotifyMembersChanged();
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
    {
        if (!m_currentLobbyID.IsValid()) return;
        if (data.m_ulSteamIDLobby != m_currentLobbyID.m_SteamID) return;
        if (data.m_bSuccess == 0) return;
        
        bool isMemberData = data.m_ulSteamIDMember != data.m_ulSteamIDLobby;
        if (isMemberData)
        {
            LobbyReadyChangedEvent?.Invoke();
            return;
        }
        
        if (SteamMatchmaking.GetLobbyData(m_currentLobbyID, GameStartedKey) != "1") return;

        string sceneName = SteamMatchmaking.GetLobbyData(m_currentLobbyID, GameSceneKey);
        if (string.IsNullOrWhiteSpace(sceneName)) sceneName = "NetworkTest";

        if (IsLobbyHost)
        {
            if (!_isGameStartRequested) StartGameForLobby(sceneName);
        }
        else
        {
            StatusChangedEvent?.Invoke("Host started the game. Connecting...");
            string hostId = SteamMatchmaking.GetLobbyData(m_currentLobbyID, HostAddressKey);
            SteamGameStartBootstrap.SetPendingStart(false, hostId, sceneName);
            PrepareForGameplaySceneLoad();
            SceneManager.LoadScene(sceneName);
        }
    }

    private void NotifyMembersChanged()
    {
        LobbyMembersChangedEvent?.Invoke(GetLobbyMemberNames());
    }

    private void PrepareForGameplaySceneLoad()
    {
        var nm = FindFirstObjectByType<FishNet.Managing.NetworkManager>();
        if (nm != null) Destroy(nm.gameObject);
    }
}