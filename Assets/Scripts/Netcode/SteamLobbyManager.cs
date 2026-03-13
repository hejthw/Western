using Steamworks;
using FishNet;
using UnityEngine;
using FishySteamworks;

public class SteamLobbyManager : MonoBehaviour
{
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;

    private CSteamID m_currentLobbyID; // ID текущего лобби (для приглашений)

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

        Debug.Log("SteamLobbyManager инициализирован, коллбэки зарегистрированы");
    }

    public void HostGame()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
    }

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Не удалось создать лобби: " + result.m_eResult);
            return;
        }

        m_currentLobbyID = new CSteamID(result.m_ulSteamIDLobby);
        Debug.Log($"Лобби создано! ID: {m_currentLobbyID}");

        SteamMatchmaking.SetLobbyData(m_currentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());

        if (InstanceFinder.ServerManager.StartConnection())
        {
            InstanceFinder.ClientManager.StartConnection();
            Debug.Log("Хост игры запущен!");
        }
        else
        {
            Debug.LogError("Не удалось запустить сервер FishNet.");
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
            Debug.LogError("Неверный ID лобби.");
        }
    }

    private void OnLobbyEntered(LobbyEnter_t result)
    {
        CSteamID lobbyId = new CSteamID(result.m_ulSteamIDLobby);
        m_currentLobbyID = lobbyId; // сохраняем ID (полезно и для клиента)
        Debug.Log("Присоединились к лобби.");

        string hostSteamIdString = SteamMatchmaking.GetLobbyData(lobbyId, "HostAddress");
        if (ulong.TryParse(hostSteamIdString, out ulong hostSteamId))
        {
            var transport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();
            transport.SetClientAddress(hostSteamId.ToString());

            if (InstanceFinder.ClientManager.StartConnection())
            {
                Debug.Log("Клиент подключается к хосту...");
            }
            else
            {
                Debug.LogError("Не удалось запустить клиента FishNet.");
            }
        }
        else
        {
            Debug.LogError("Не удалось получить SteamID хоста из данных лобби.");
        }
    }

    // Приглашение от друга
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t param)
    {
        Debug.Log($"Получен запрос на присоединение к лобби: {param.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(param.m_steamIDLobby);
    }

    // Кнопка "Пригласить друга"
    public void InviteFriend()
    {
        if (m_currentLobbyID.IsValid())
        {
            SteamFriends.ActivateGameOverlayInviteDialog(m_currentLobbyID);
            Debug.Log("Открыто окно приглашения друзей");
        }
        else
        {
            Debug.LogError("Нет активного лобби для приглашения");
        }
    }
}