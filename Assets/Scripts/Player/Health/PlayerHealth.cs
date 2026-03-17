using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine.Events;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private readonly SyncVar<int> _health = new SyncVar<int>();
    
    public event UnityAction<int> onHealthChange;

    private void Awake()
    {
        _health.OnChange += on_health;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        if (IsServerInitialized)
            _health.Value = maxHealth;
    }

    private void on_health(int prev, int next, bool asServer)
    {
        if (!asServer)
        {
            onHealthChange?.Invoke(next);
            Debug.Log($"CLIENT HP:{prev} - {next}");
        }
        else
        {
            Debug.Log($"SERVER HP:{prev} - {next}");
        }
    }

    [Server]
    public void TakeDamage(int amount)
    {
        _health.Value -= amount;
    }
    
}