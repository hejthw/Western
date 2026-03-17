using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
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

    private void on_health(int prev, int next, bool asServer)
    {
        if (!asServer)
        {
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