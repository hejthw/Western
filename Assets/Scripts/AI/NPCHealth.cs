using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class NPCHealth : NetworkBehaviour
{
    [SerializeField] private NPCHealthData data;
    
    private readonly SyncVar<int> _health = new SyncVar<int>();
    private readonly SyncVar<bool> _isDead = new SyncVar<bool>();
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        _health.OnChange += OnHealthChanged;

        if (IsServerInitialized)
        {
            _health.Value = data.maxHealth;
        }
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"Health changed from {prev} to {next}");
    }

    [Server]
    public void TakeDamage(int amount)
    {
        if (_isDead.Value) return;

        _health.Value -= amount;

        if (_health.Value <= 0)
            Death();
    }

    [Server]
    private void Death()
    {
        _isDead.Value = true;
        NPCEvents.RaiseDeadEvent();
    }
}