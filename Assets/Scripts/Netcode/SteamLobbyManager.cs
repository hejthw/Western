using Steamworks;
using FishNet;
using UnityEngine;
using FishySteamworks;

public class SteamLobbyManager : MonoBehaviour
{
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;

    private CSteamID m_currentLobbyID; 

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

        Debug.Log("SteamLobbyManager initialized");
    }

    public void HostGame()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
    }

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create a lobby " + result.m_eResult);
            return;
        }

        m_currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        Debug.Log($"Lobby created ID: {m_currentLobbyID}");

        SteamMatchmaking.SetLobbyData(m_currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());

        if (InstanceFinder.ServerManager.StartConnection())
        {
            InstanceFinder.ClientManager.StartConnection();
            Debug.Log("Host active");
        }
        else
        {
            Debug.LogError("Cant connect to fishnet");
        }
    }

    public void JoinGame(string lobbyIdString)
    {
        if (ulong.TryParse(lobbyIdString, out ulong id))
        {
            CSteamID lobbyId = new CSteamID(id);
            SteamMatchmaking.JoinLobby(lobbyId);
        }
        else
        {
            Debug.LogError("Wrong ID");
        }
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        CSteamID lobbyId = new CSteamID(result.m_ulSteamIDLobby);
        m_currentLobbyID = lobbyId; 


        string hostSteamIdString = SteamMatchmaking.GetLobbyData(lobbyId, "HostAddress");
        if (ulong.TryParse(hostSteamIdString, out ulong hostSteamId))
        {
            var transport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();
            transport.SetClientAddress(hostSteamId.ToString());

            if (InstanceFinder.ClientManager.StartConnection())
            {
                Debug.Log("Client connecting to host...");
            }
            else
            {
                Debug.LogError("Cant laucnh FishNet.");
            }
        }
        else
        {
            Debug.LogError("Cant get host Steamid");
        }
    }

    
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t param)
    {
      
        SteamMatchmaking.JoinLobby(param.m_steamIDLobby);
    }

  
    public void InviteFriend()
    {
        if (m_currentLobbyID.IsValid())
        {
            SteamFriends.ActivateGameOverlayInviteDialog(m_currentLobbyID);
        }
        else
        {
            Debug.LogError("No active lobby");
        }
    }
}