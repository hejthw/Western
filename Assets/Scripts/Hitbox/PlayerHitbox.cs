using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private HitboxType hitboxType;
    [SerializeField] private PlayerHealth ownerHealth;
    
    public PlayerHealth OwnerHealth => ownerHealth;
    
    public float GetMultiplier() => hitboxType switch
    {
        HitboxType.Head  => 0.15f,
        HitboxType.Torso => 0.075f,
        HitboxType.Arms => 0.02f,
        HitboxType.Legs => 0.02f,
        _ => 1.0f
    };
}