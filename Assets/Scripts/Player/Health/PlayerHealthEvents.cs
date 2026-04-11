using System;

public static class PlayerHealthEvents
{
    public static event Action<int> OnLocalHealthChange;
    public static event Action<bool> OnDeadEvent;
    public static event Action<bool> OnKnockoutEvent;
    public static event Action RespawnEvent; // 
    public static event System.Action<bool> OnStunnedEvent;

    public static void RaiseRespawnEvent() => RespawnEvent?.Invoke();
    public static void RaiseDeadEvent(bool isDead) => OnDeadEvent?.Invoke(isDead);
    public static void RaiseHealthChange(int amount) => OnLocalHealthChange?.Invoke(amount);
    public static void RaiseKnockoutEvent(bool isKnocked) => OnKnockoutEvent?.Invoke(isKnocked);
    public static void RaiseStunnedEvent(bool stunned) => OnStunnedEvent?.Invoke(stunned);
}