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
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 40f;
    [SerializeField] private float  knockoutDelay = 5f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject localUI;

    public readonly SyncVar<int> _health = new SyncVar<int>();
    public readonly SyncVar<bool> _isKnockout = new SyncVar<bool>();
    public readonly SyncVar<bool> _isDead = new SyncVar<bool>();
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _health.OnChange += OnHealthChanged;
        _isDead.OnChange += OnDeadChanged;
        _isKnockout.OnChange += OnKnockoutChanged;

        if (IsServerInitialized)
        {
            _health.Value = maxHealth;
            _isDead.Value = false;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        localUI.SetActive(IsOwner);
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        if (asServer) return;
        if (!IsOwner) return;
        PlayerEvents.RaiseHealthChange(next);
    }

    private void OnKnockoutChanged(bool prev, bool next, bool asServer)
    {
        if (!IsOwner) return;

        if (next)
        {
            PlayerEvents.RaiseKnockoutEvent(true);
        }
        else 
            PlayerEvents.RaiseKnockoutEvent(false);
    }

    private void OnDeadChanged(bool prev, bool next, bool asServer)
    {
        if (!IsOwner) return;
        
        if (next)
            PlayerEvents.RaiseDeadEvent(true);
        else
            PlayerEvents.RaiseDeadEvent(false);
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (_isDead.Value) return;

        _health.Value -= amount;

        if (_health.Value <= 0)
            Knockout();
    }

    [Server]
    private void Knockout()
    {
        _health.Value = 0;
        _isKnockout.Value = true;
        
        StartCoroutine(KnockoutCoroutine());
    }
    
    [Server]
    private IEnumerator KnockoutCoroutine()
    {
        yield return new WaitForSeconds(knockoutDelay);
        Death();
    }

    [Server]
    private void Death()
    {
        _isDead.Value = true;
        _health.Value = -1;
        
        StartCoroutine(DeadCoroutine());
    }
    
    [Server]
    private IEnumerator DeadCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    [Server]
    private void Respawn()
    {
        Vector3 spawnPos = GetSpawnPosition();
        transform.position = spawnPos;

        _health.Value = maxHealth;
        _isKnockout.Value = false;
        _isDead.Value = false;

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