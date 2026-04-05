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
    
    

    
    
    public void TryShoot(Transform player, bool playerIsMoving)
    {

    }
    
    
    //
    // Transform SelectTarget(List<Transform> players)
    // {
    //     Transform best = null;
    //     float bestScore = float.MinValue;
    //
    //     foreach (var p in players)
    //     {
    //         float dist = Vector3.Distance(transform.position, p.position);
    //         float hp = p.GetComponent<PlayerHealth>().GetHealth();
    //
    //         // Ближе = опаснее, больше урон = приоритет, мало HP = добить
    //         float score = (1f / dist) * 2f + (1f - hp / 100f) * 1f;
    //
    //         if (score > bestScore) { bestScore = score; best = p; }
    //     }
    //     return best;
    // }
    
}