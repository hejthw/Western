using UnityEngine;
using TMPro;

public class PlayerHealthView : MonoBehaviour
{
    public TMP_Text HealthText;
    
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
        HealthText.text = health.ToString();
    }
    
}