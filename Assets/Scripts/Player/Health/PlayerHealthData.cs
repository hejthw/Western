using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/PlayerHealthData")]
public class PlayerHealthData : ScriptableObject
{
    public int maxHealth = 100;
    public float  knockoutDelay = 5f;
    public float respawnDelay = 40f;
}