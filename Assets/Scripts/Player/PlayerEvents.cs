using System;

public static class PlayerEvents
{
    public static event Action NextTargetEvent;
    public static event Action PrevTargetEvent;
    public static event Action<SuspicionType> OnSuspicion;
    
    public static void RaiseSuspicion(SuspicionType type)
    {
        OnSuspicion?.Invoke(type);
    }

    public static void RaiseNextTargetEvent()
    {
        NextTargetEvent?.Invoke();
    }

    public static void RaisePrevTargetEvent()
    {
        PrevTargetEvent?.Invoke();
    }
    
    public static event Action<PlayerName, string> OnPlayerRegistered;
    public static event Action<PlayerName> OnPlayerUnregistered;
    public static event Action<PlayerName, string> OnPlayerNameChanged;

    public static void RaisePlayerRegistered(PlayerName player, string name)
        => OnPlayerRegistered?.Invoke(player, name);

    public static void RaisePlayerUnregistered(PlayerName player)
        => OnPlayerUnregistered?.Invoke(player);

    public static void RaisePlayerNameChanged(PlayerName player, string name)
        => OnPlayerNameChanged?.Invoke(player, name);
}