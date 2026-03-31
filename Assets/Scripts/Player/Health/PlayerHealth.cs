using System;
using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerHealthData data;
    [SerializeField] private Transform[] spawnPoints;
    
    private readonly SyncVar<int> _health = new SyncVar<int>();
    private readonly SyncVar<PlayerHealthState> _state = new SyncVar<PlayerHealthState>();
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _health.OnChange += OnHealthChanged;
        _state.OnChange += OnStateChanged;

        if (IsServerInitialized)
        {
            _health.Value = data.maxHealth;
            _state.Value = PlayerHealthState.Alive;
        }
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        if (asServer) return;
        if (!IsOwner) return;
        PlayerHealthEvents.RaiseHealthChange(next);
    }

    private void OnStateChanged(PlayerHealthState prev, PlayerHealthState next, bool asServer)
    {
        if (!IsOwner) return;

        if (next == PlayerHealthState.Knockout)
            PlayerHealthEvents.RaiseKnockoutEvent(true);
        else if (next == PlayerHealthState.Dead)
            PlayerHealthEvents.RaiseDeadEvent(true);
        else
        {
            PlayerHealthEvents.RaiseDeadEvent(false);
            PlayerHealthEvents.RaiseKnockoutEvent(false);
        }
        
    }

    // private void OnKnockoutChanged(bool prev, bool next, bool asServer)
    // {
    //     if (!IsOwner) return;
    //
    //     if (next)
    //     {
    //         
    //     }
    //     else 
    //         PlayerEvents.RaiseKnockoutEvent(false);
    // }
    //
    // private void OnDeadChanged(bool prev, bool next, bool asServer)
    // {
    //     if (!IsOwner) return;
    //     
    //     if (next)
    //         PlayerEvents.RaiseDeadEvent(true);
    //     else
    //         PlayerEvents.RaiseDeadEvent(false);
    // }

    [Server]
    public void TakeDamage(int amount)
    {
        if (_state.Value == PlayerHealthState.Dead) return;

        _health.Value -= amount;

        if (_health.Value <= 0)
            Knockout();
    }

    [Server]
    private void Knockout()
    {
        _health.Value = 0;
        _state.Value = PlayerHealthState.Knockout;
        
        StartCoroutine(KnockoutCoroutine());
    }
    
    [Server]
    private IEnumerator KnockoutCoroutine()
    {
        yield return new WaitForSeconds(data.knockoutDelay);
        Death();
    }

    [Server]
    private void Death()
    {
        _state.Value = PlayerHealthState.Dead;
        _health.Value = -1;
        
        StartCoroutine(DeadCoroutine());
    }
    
    [Server]
    private IEnumerator DeadCoroutine()
    {
        yield return new WaitForSeconds(data.respawnDelay);
        Respawn();
    }

    [Server]
    private void Respawn()
    {
        Vector3 spawnPos = GetSpawnPosition();
        transform.position = spawnPos;

        _health.Value = data.maxHealth;
        _state.Value = PlayerHealthState.Alive;

        RpcOnRespawned(spawnPos);
    }

    [ObserversRpc]
    private void RpcOnRespawned(Vector3 position)
    {
        transform.position = position;
        Debug.Log($"{gameObject.name} respawned at {position}");
    }
    

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        return Vector3.zero;
    }
}