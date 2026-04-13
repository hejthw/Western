using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    
    [Header("Team HUD")]
    [SerializeField] private Transform teamHUDContainer;
    [SerializeField] private TeamHUDEntry teamEntryPrefab;

    private readonly Dictionary<PlayerName, TeamHUDEntry> _teamEntries = new();

    private void OnEnable()
    {
        PlayerHealthEvents.OnLocalHealthChange       += UpdateHealthText;
        PlayerEvents.OnPlayerRegistered      += OnTeammateJoined;
        PlayerEvents.OnPlayerUnregistered    += OnTeammateLeft;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnLocalHealthChange       -= UpdateHealthText;
        PlayerEvents.OnPlayerRegistered      -= OnTeammateJoined;
        PlayerEvents.OnPlayerUnregistered    -= OnTeammateLeft;
    }

    public override void OnStartClient()
    {
        if (!IsOwner)
            gameObject.SetActive(false);
    }

    private void OnTeammateJoined(PlayerName identity, string name)
    {
        // PlayerHealth лежит на том же GameObject
        var health = identity.GetComponent<PlayerHealth>();
        if (health == null) return;

        var entry = Instantiate(teamEntryPrefab, teamHUDContainer);
        entry.Track(health, identity, name);
        _teamEntries[identity] = entry;
    }

    private void OnTeammateLeft(PlayerName identity)
    {
        if (!_teamEntries.TryGetValue(identity, out var entry)) return;

        entry.Untrack();
        Destroy(entry.gameObject);
        _teamEntries.Remove(identity);
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
}