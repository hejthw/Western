using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    public bool IsMain;
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        string myName = SteamFriends.GetPersonaName();
        nameText.text = myName;
        SetPlayerName(myName);
        healthText.text = "100";
    }
    
    [ServerRpc]
    private void SetPlayerName(string playerName)
    {
        SetPlayerNameForObservers(playerName);
    }

    [ObserversRpc(BufferLast = true)]
    private void SetPlayerNameForObservers(string playerName)
    {
        nameText.text = playerName;
    }

}