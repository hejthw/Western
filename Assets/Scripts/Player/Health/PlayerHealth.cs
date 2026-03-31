using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerHealthData data;
    
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
        
        _state.Value = PlayerHealthState.Alive;
        _health.Value = data.maxHealth;
        PlayerHealthEvents.RaiseRespawnEvent();
    }
}