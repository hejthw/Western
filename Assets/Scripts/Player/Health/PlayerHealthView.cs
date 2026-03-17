using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using FishNet.Object.Synchronizing;
using TMPro;

public class PlayerHealthView : MonoBehaviour
{
    public TMP_Text HealthText;
    
    public PlayerHealth playerHealth;
    
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