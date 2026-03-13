using UnityEngine;
using TMPro; // обязательно для TMP

public class JoinButtonHandler : MonoBehaviour
{
    public SteamLobbyManager lobbyManager;
    public TMP_InputField inputField; // для TextMeshPro

    public void OnJoinButtonClick()
    {
        if (lobbyManager != null && inputField != null)
        {
            lobbyManager.JoinGame(inputField.text);
        }
    }
}