using TMPro;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private TMP_Text HealthText;
    
    private void OnEnable()
    {
        PlayerHealthEvents.OnLocalHealthChange += HandleHealthChange;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnLocalHealthChange -= HandleHealthChange;
    }

    private void HandleHealthChange(int health)
    {
        if (health == -1)
            HealthText.text = "Dead";
        else if (health != 0)
            HealthText.text = $"Health: {health.ToString()}";
        else
            HealthText.text = "Knockout!";
    }
    
}