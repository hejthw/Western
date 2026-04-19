using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System.Collections;
using Steamworks;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerHealthData data;
    
    private readonly SyncVar<int> _health = new SyncVar<int>();
    private readonly SyncVar<PlayerHealthState> _state = new SyncVar<PlayerHealthState>();
    
    public int GetHealth() => _health.Value;
    
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
        if (IsOwner) PlayerHealthEvents.RaiseHealthChange(next);
        else PlayerHealthEvents.RaiseTeammateHealthChange(this, next);
    }

    private void OnStateChanged(PlayerHealthState prev, PlayerHealthState next, bool asServer)
    {
        if (IsOwner)
        {
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
        else
        {
            PlayerHealthEvents.RaiseTeammateStateChange(this, next);
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

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
            pc.ForceDropWeapon();
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.5f;
            inventory.DropAllItems(dropPos);
        }

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