using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/PlayerHealthData")]
public class PlayerHealthData : ScriptableObject
{
    public int maxHealth = 100;
    public float knockoutDelay = 20f;
    public float respawnDelay = 40f;

    [Header("Revive")] 
    public float wakeupWindow = 5f;
    public int hpToGain = 30;
}