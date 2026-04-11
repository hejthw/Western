using UnityEngine;
using TMPro;

public class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private TMP_Text HealthText;

    private void OnEnable()
    {
        PlayerHealthEvents.OnLocalHealthChange += HandleHealthChange;
        //PlayerHealthEvents.OnStunnedEvent += HandleStunned;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnLocalHealthChange -= HandleHealthChange;
       // PlayerHealthEvents.OnStunnedEvent -= HandleStunned;
    }

    private void HandleHealthChange(int health)
    {
        if (health == -1)
            HealthText.text = "Dead";
        else if (health > 0)
            HealthText.text = $"Health: {health}";
        else
            HealthText.text = "Knockout!";
    }

    private void HandleStunned(bool stunned)
    {
        if (stunned)
            HealthText.text = "STUNNED!";

    }
}