using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    
    [Header("Team HUD")]
    [SerializeField] private Transform teamHUDContainer;
    [SerializeField] private TeamHUDEntry teamEntryPrefab;

    private readonly Dictionary<PlayerHealth, TeamHUDEntry> _teamEntries = new();

    private void OnEnable()
    {
        PlayerHealthEvents.OnLocalHealthChange     += UpdateHealthText;
        PlayerHealthEvents.OnTeammateRegistered    += OnTeammateJoined;
        PlayerHealthEvents.OnTeammateUnregistered  += OnTeammateLeft;
        PlayerHealthEvents.OnLocalHealthChange += UpdateHealthText;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnLocalHealthChange     -= UpdateHealthText;
        PlayerHealthEvents.OnTeammateRegistered    -= OnTeammateJoined;
        PlayerHealthEvents.OnTeammateUnregistered  -= OnTeammateLeft;
        PlayerHealthEvents.OnLocalHealthChange -= UpdateHealthText;
    }

    private void OnTeammateJoined(PlayerHealth player)
    {
        var nameView = player.GetComponent<PlayerNameView>();
        string playerName = nameView != null ? nameView.PlayerName.Value : "Unknown";

        var entry = Instantiate(teamEntryPrefab, teamHUDContainer);
        entry.Track(player, playerName);
        _teamEntries[player] = entry;
    }

    private void OnTeammateLeft(PlayerHealth player)
    {
        if (!_teamEntries.TryGetValue(player, out var entry)) return;
    
        entry.Untrack();
        Destroy(entry.gameObject);
        _teamEntries.Remove(player);
    }
    
    
    public void Awake()
    {
        string myName = SteamFriends.GetPersonaName();
        nameText.text = myName;
        healthText.text = "100";
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