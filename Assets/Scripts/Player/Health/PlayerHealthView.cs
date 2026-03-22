using UnityEngine;
using TMPro;

public class PlayerHealthView : MonoBehaviour
{
    [SerializeField] private TMP_Text HealthText;
    
    private void OnEnable()
    {
        PlayerEvents.OnLocalHealthChange += HandleHealthChange;
    }

    private void OnDisable()
    {
        PlayerEvents.OnLocalHealthChange -= HandleHealthChange;
    }

    private void HandleHealthChange(int health)
    {
        if (health != 0)
            HealthText.text = $"Health: {health.ToString()}";
        else
            HealthText.text = "You are Dead!";
    }
    
}