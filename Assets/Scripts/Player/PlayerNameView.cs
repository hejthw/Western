using FishNet.Object;
using Steamworks;
using TMPro;
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
            name = SteamFriends.GetPersonaName();
            text.text = name;
            SetPlayerName(name);
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