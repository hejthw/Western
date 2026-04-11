using System;

public static class PlayerHealthEvents
{
    public static event Action<int> OnLocalHealthChange;
    public static event Action<bool> OnDeadEvent;
    public static event Action<bool> OnKnockoutEvent;
    public static event Action RespawnEvent;
    
    public static event Action<PlayerHealth, int> OnTeammateHealthChange;
    public static event Action<PlayerHealth, PlayerHealthState> OnTeammateStateChange;
    public static event Action<PlayerHealth, string> OnTeammateRegistered;
    public static event Action<PlayerHealth> OnTeammateUnregistered;

    public static void RaiseTeammateHealthChange(PlayerHealth player, int health) 
        => OnTeammateHealthChange?.Invoke(player, health);

    public static void RaiseTeammateStateChange(PlayerHealth player, PlayerHealthState state) 
        => OnTeammateStateChange?.Invoke(player, state);
    
    public static void RaiseTeammateRegistered(PlayerHealth player, string name) => OnTeammateRegistered?.Invoke(player, name);
    public static void RaiseTeammateUnregistered(PlayerHealth player) => OnTeammateUnregistered?.Invoke(player);
    
    public static void RaiseRespawnEvent() => RespawnEvent?.Invoke();
    public static void RaiseDeadEvent(bool isDead) => OnDeadEvent?.Invoke(isDead);
    public static void RaiseHealthChange(int amount) => OnLocalHealthChange?.Invoke(amount);
    public static void RaiseKnockoutEvent(bool isKnocked) => OnKnockoutEvent?.Invoke(isKnocked);
}