using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine.Events;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject _localUI;
    
    private readonly SyncVar<int> _health = new SyncVar<int>();

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

    public override void OnStartClient()
    {
        base.OnStartClient();
        _localUI.SetActive(IsOwner);
    }

    private void on_health(int prev, int next, bool asServer)
    {
        if (asServer) return;
        if (IsOwner)
        {
            PlayerEvents.RaiseHealthChange(next);
            Debug.Log($"Health changed to {next}");
        }
        else
        {
        }
    }

    [Server]
    public void TakeDamage(int amount)
    {
        _health.Value -= amount;
    }
    
}