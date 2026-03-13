using FishNet.Connection;
using FishNet.Object;
using TMPro;
using FishySteamworks;
using Steamworks;
using UnityEngine;

public class PlayerNameView : NetworkBehaviour
{
    [SerializeField] private TMP_Text text;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            string myName = SteamFriends.GetPersonaName();
            text.text = myName;
            SetPlayerNameForObservers(myName);
        }
    }
    
    [ServerRpc]
    private void SetPlayerName(string playerName)
    {
        SetPlayerNameForObservers(playerName);
    }

    [ObserversRpc(BufferLast = true)]
    private void SetPlayerNameForObservers(string playerName)
    {
        text.text = playerName;
    }

}