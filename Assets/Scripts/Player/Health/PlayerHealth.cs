using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private GameObject _localUI;
    
    private readonly SyncVar<int> _health = new SyncVar<int>();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        _health.OnChange += on_health;
        
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
        Debug.Log($"on_health | asServer={asServer} | IsOwner={IsOwner} | IsClientInitialized={IsClientInitialized} | next={next}");
        if (asServer) return;
        if (!IsOwner) return;
        PlayerEvents.RaiseHealthChange(next);
        Debug.Log($"Health changed to {next}");
    }

    [Server]
    public void TakeDamage(int amount)
    {
        _health.Value -= amount;
    }
    
}