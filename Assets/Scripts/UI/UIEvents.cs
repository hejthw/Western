using System;

public static class UIEvents
{
    public static event Action<LassoHUDState> OnLassoStateChanged;
    public static event Action<bool> OnCashZoneChanged;

    public static void RaiseOnLassoStateChanged(LassoHUDState state)
    {
        OnLassoStateChanged?.Invoke(state);
    }

    public static void RaiseOnCashZoneChanged(bool state)
    {
        OnCashZoneChanged?.Invoke(state);
    }
}