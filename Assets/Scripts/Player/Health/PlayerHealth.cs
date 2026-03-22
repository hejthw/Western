using System;
using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System.Collections;
using Random = UnityEngine.Random;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject _localUI;

    private readonly SyncVar<int> _health = new SyncVar<int>();
    private readonly SyncVar<bool> _isDead = new SyncVar<bool>();

    

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _health.OnChange += OnHealthChanged;
        _isDead.OnChange += OnDeadChanged;

        if (IsServerInitialized)
        {
            _health.Value = maxHealth;
            _isDead.Value = false;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _localUI.SetActive(IsOwner);
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        if (asServer) return;
        if (!IsOwner) return;
        PlayerEvents.RaiseHealthChange(next);
    }

    private void OnDeadChanged(bool prev, bool next, bool asServer)
    {
        if (next)
            OnDied(asServer);
        else
            OnRespawned(asServer);
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (_isDead.Value) return;

        _health.Value -= amount;

        if (_health.Value <= 0)
            Die();
    }

    [Server]
    private void Die()
    {
        _health.Value = 0;
        _isDead.Value = true;

        RpcOnDied();

        StartCoroutine(RespawnCoroutine());
    }

    [ObserversRpc]
    private void RpcOnDied()
    {
        // Визуальная реакция на смерть — отключение модели, эффект и т.д.
        Debug.Log($"{gameObject.name} died");
    }

    [Server]
    private IEnumerator RespawnCoroutine()
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
        _isDead.Value = false;

        RpcOnRespawned(spawnPos);
    }

    [ObserversRpc]
    private void RpcOnRespawned(Vector3 position)
    {
        transform.position = position;
        Debug.Log($"{gameObject.name} respawned at {position}");
    }

    private void OnDied(bool asServer)
    {
        PlayerEvents.RaiseDeadEvent(true);
    }

    private void OnRespawned(bool asServer)
    {
        PlayerEvents.RaiseDeadEvent(false);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        return Vector3.zero;
    }
}