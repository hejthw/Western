using UnityEngine;

[CreateAssetMenu(fileName = "ObstaclesSettingsData", menuName = "ScriptableObjects/ObstaclesSettingsData")]
public class ObstaclesSettingsData : ScriptableObject
{
    [Header("Explosion")]
    public float explosionForce = 10f;
    public float explosionRadius = 5f;
}