using FishNet.Object;
using TMPro;
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
            text.gameObject.SetActive(false);
            string myName = SteamFriends.GetPersonaName();
            text.text = myName;
            SetPlayerName(myName);
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