using System;
using UnityEngine;

public class NPCHitbox : MonoBehaviour
{
    [SerializeField] private HitboxType hitboxType;
    [SerializeField] private NPCHealth ownerHealth;
    
    public NPCHealth OwnerHealth => ownerHealth;
    
    public float GetMultiplier() => hitboxType switch
    {
        HitboxType.Head  => 1.0f,
        HitboxType.Torso => 0.5f,
        HitboxType.Arms => 0.25f,
        HitboxType.Legs => 0.25f,
        _ => 1.0f
    };
}