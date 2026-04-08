using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TeamHUDEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;

    private PlayerNameView _nameView;
    private PlayerHealth _tracked;

    private void Awake()
    {
        _nameView = GetComponent<PlayerNameView>();
    }

    public bool IsTracking(PlayerHealth player) => _tracked == player;

    public void Track(PlayerHealth player, string playerName)
    {
        _tracked = player;
        nameText.text = playerName;
        StartCoroutine(WaitForName());
        Refresh(player.GetHealth());

        PlayerHealthEvents.OnTeammateHealthChange += OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange  += OnStateChanged;
    }

    public void Untrack()
    {
        PlayerHealthEvents.OnTeammateHealthChange -= OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange  -= OnStateChanged;
        _tracked = null;
    }

    private IEnumerator WaitForName()
    {
        yield return new WaitForSeconds(2f);
        nameText.text = _nameView.PlayerName.Value;
    }

    private void OnHealthChanged(PlayerHealth player, int health)
    {
        if (player != _tracked) return;
        Refresh(health);
    }

    private void OnStateChanged(PlayerHealth player, PlayerHealthState state)
    {
        if (player != _tracked) return;

        healthText.text = state switch
        {
            PlayerHealthState.Knockout => "Knock",
            PlayerHealthState.Dead     => "Dead",
            _                          => _tracked.GetHealth().ToString()
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