using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enforcer: NetworkNPC
{
    [SerializeField] private float _criticalRange = 3f;
    [SerializeField] private float damage = 100f;
    
    private PlayerHitbox _currentPlayerHitbox;
    
    public float GetHitChance(float distance, bool playerIsMoving)
    {
        if (distance <= _criticalRange) return 0.75f;
        return playerIsMoving ? 0.3f : 0.6f;
    }

    HitboxType ChooseBodyPart()
    {
        var value = (float)Random.value;

        if (value > 0.7f) return HitboxType.Arms;
        if (value > 0.25f) return HitboxType.Torso;
        return HitboxType.Head;
    }
    
    public void TryShoot(Transform player, bool playerIsMoving)
    {
        float dist = Vector3.Distance(transform.position, player.position);
        float hitChance = GetHitChance(dist, playerIsMoving);

        if (Random.value <= hitChance)
        {
            HitboxType hbType = ChooseBodyPart();
            int finalDamage = Mathf.RoundToInt(damage * _currentPlayerHitbox.GetMultiplier());
            player.GetComponent<PlayerHealth>().TakeDamage(finalDamage);
        }
    }
    
    Transform SelectTarget(List<Transform> players)
    {
        Transform best = null;
        float bestScore = float.MinValue;

        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.position);
            float hp = p.GetComponent<PlayerHealth>().GetHealth();

            // Ближе = опаснее, больше урон = приоритет, мало HP = добить
            float score = (1f / dist) * 2f + (1f - hp / 100f) * 1f;

            if (score > bestScore) { bestScore = score; best = p; }
        }
        return best;
    }
    
}