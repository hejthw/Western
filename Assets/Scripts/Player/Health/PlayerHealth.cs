using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;
using System.Collections;
using FishNet.Connection;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerHealthData data;

    private readonly SyncVar<int> _health = new SyncVar<int>();
    private readonly SyncVar<PlayerHealthState> _state = new SyncVar<PlayerHealthState>();

    // --- Revive state (server only) ---
    private int _reviveGlassCount = 0;
    private float _reviveWindowTimer = 0f;
    private bool _reviveWindowOpen = false;
    private Coroutine _knockoutCoroutine;

    public int GetHealth() => _health.Value;
    public bool IsKnockedOut => _state.Value == PlayerHealthState.Knockout;

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

        _reviveGlassCount = 0;
        _reviveWindowOpen = false;
        _reviveWindowTimer = 0f;

        _knockoutCoroutine = StartCoroutine(KnockoutCoroutine());
    }

    [Server]
    private IEnumerator KnockoutCoroutine()
    {
        yield return new WaitForSeconds(data.knockoutDelay);
        Death();
    }

    // Вызывается из WhiskeyGlass при попадании в нокнутого
    [Server]
    public void RegisterReviveGlass()
    {
        Debug.Log("RegisterReviveGlass");
        if (_state.Value != PlayerHealthState.Knockout) return;

        if (!_reviveWindowOpen)
        {
            _reviveWindowOpen = true;
            _reviveGlassCount = 1;
            StartCoroutine(ReviveWindowCoroutine());
        }
        else
        {
            _reviveGlassCount++;
        }
    }

    [Server]
    private IEnumerator ReviveWindowCoroutine()
    {
        yield return new WaitForSeconds(data.wakeupWindow);

        // Окно закрылось — воскрешаем
        if (_state.Value == PlayerHealthState.Knockout)
            Revive(_reviveGlassCount);

        _reviveWindowOpen = false;
        _reviveGlassCount = 0;
        _reviveWindowTimer = 0f;
    }

    [Server]
    private void Revive(int glassCount)
    {
        // Отменяем таймер смерти
        if (_knockoutCoroutine != null)
        {
            StopCoroutine(_knockoutCoroutine);
            _knockoutCoroutine = null;
        }

        int hp = Mathf.Min(glassCount * data.hpToGain, 100);
        _health.Value = hp;
        _state.Value = PlayerHealthState.Alive;

        // +1 стак опьянения воскрешённому
        RpcRaiseWhiskeyOnOwner(Owner);
    }

    // Вызываем событие опьянения на клиенте владельца
    [TargetRpc]
    private void RpcRaiseWhiskeyOnOwner(NetworkConnection conn)
    {
        Debug.Log("HERE");
        PlayerEffectsEvents.RaiseWhiskeyUse();
    }

    [Server]
    private void Death()
    {
        _state.Value = PlayerHealthState.Dead;
        _health.Value = -1;
        
    }
    
    // [Server]
    // private IEnumerator DeadCoroutine()
    // {
    //     yield return new WaitForSeconds(data.respawnDelay);
    //
    //     _state.Value = PlayerHealthState.Alive;
    //     _health.Value = data.maxHealth;
    //     PlayerHealthEvents.RaiseRespawnEvent();
    // }
}