using TMPro;
using UnityEngine;

public class TeamHUDEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;

    private PlayerHealth _trackedHealth;
    private PlayerName _trackedIdentity;

    public bool IsTracking(PlayerHealth player) => _trackedHealth == player;

    public void Track(PlayerHealth health, PlayerName identity, string playerName)
    {
        _trackedHealth   = health;
        _trackedIdentity = identity;

        nameText.text = string.IsNullOrEmpty(playerName) ? "..." : playerName;
        Refresh(health.GetHealth());

        PlayerHealthEvents.OnTeammateHealthChange  += OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange   += OnStateChanged;
        PlayerEvents.OnPlayerNameChanged   += OnNameChanged;
    }

    public void Untrack()
    {
        PlayerHealthEvents.OnTeammateHealthChange  -= OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange   -= OnStateChanged;
        PlayerEvents.OnPlayerNameChanged   -= OnNameChanged;

        _trackedHealth   = null;
        _trackedIdentity = null;
    }

    private void OnNameChanged(PlayerName identity, string name)
    {
        if (identity != _trackedIdentity) return;
        if (!string.IsNullOrEmpty(name))
            nameText.text = name;
    }

    private void OnHealthChanged(PlayerHealth player, int health)
    {
        if (player != _trackedHealth) return;
        Refresh(health);
    }

    private void OnStateChanged(PlayerHealth player, PlayerHealthState state)
    {
        if (player != _trackedHealth) return;

        healthText.text = state switch
        {
            PlayerHealthState.Knockout => "Knock",
            PlayerHealthState.Dead     => "Dead",
            _                          => _trackedHealth.GetHealth().ToString()
        };
    }

    private void Refresh(int hp)
    {
        healthText.text = hp switch
        {
            -1 => "Dead",
            0  => "Knock",
            _  => hp.ToString()
        };
    }

    private void OnDestroy() => Untrack();
}