using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private HitboxType hitboxType;
    [SerializeField] private PlayerHealth ownerHealth;
    
    public PlayerHealth OwnerHealth => ownerHealth;
    
    public float GetMultiplier() => hitboxType switch
    {
        HitboxType.Head  => 0.05f,
        HitboxType.Torso => 0.1f,
        HitboxType.Arms => 0.03f,
        HitboxType.Legs => 0.03f,
        _ => 1.0f
    };
}