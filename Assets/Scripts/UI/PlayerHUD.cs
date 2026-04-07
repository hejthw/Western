using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    public bool IsMain;
    
    public void Awake()
    {
        string myName = SteamFriends.GetPersonaName();
        nameText.text = myName;
        // SetPlayerName(myName);
        healthText.text = "100";
    }
    
    private void OnEnable()
    {
        PlayerHealthEvents.OnLocalHealthChange += UpdateHealthText;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnLocalHealthChange -= UpdateHealthText;
    }

    private void UpdateHealthText(int amount)
    {
        if (amount == -1)
            healthText.text = "Dead";
        else if (amount != 0)
            healthText.text = amount.ToString();
        else
            healthText.text = "Knock";
    }
    
    // [ServerRpc]
    // private void SetPlayerName(string playerName)
    // {
    //     SetPlayerNameForObservers(playerName);
    // }
    //
    // [ObserversRpc(BufferLast = true)]
    // private void SetPlayerNameForObservers(string playerName)
    // {
    //     nameText.text = playerName;
    // }

}