using TMPro;
using UnityEngine;

public class TeamHUDEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;

    private PlayerHealth _tracked;

    public bool IsTracking(PlayerHealth player) => _tracked == player;

    public void Track(PlayerHealth player, string playerName)
    {
        _tracked = player;
    
        // Подписываемся на изменение имени
        var nameView = player.GetComponent<PlayerNameView>();
        if (nameView != null)
        {
            nameView.PlayerName.OnChange += OnNameChanged;
            // Берём актуальное имя если уже пришло, иначе дождёмся колбэка
            playerName = string.IsNullOrEmpty(nameView.PlayerName.Value) 
                ? "..." 
                : nameView.PlayerName.Value;
        }

        nameText.text = playerName;
        Refresh(player.GetHealth());

        PlayerHealthEvents.OnTeammateHealthChange += OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange  += OnStateChanged;
    }

    public void Untrack()
    {
        var nameView = _tracked?.GetComponent<PlayerNameView>();
        if (nameView != null)
            nameView.PlayerName.OnChange -= OnNameChanged;

        PlayerHealthEvents.OnTeammateHealthChange -= OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange  -= OnStateChanged;
        _tracked = null;
    }

    private void OnNameChanged(string prev, string next, bool asServer)
    {
        nameText.text = next;
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