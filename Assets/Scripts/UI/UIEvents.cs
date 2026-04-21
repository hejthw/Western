using System;

public static class UIEvents
{
    public static event Action<LassoHUDState> OnLassoStateChanged;

    public static void RaiseOnLassoStateChanged(LassoHUDState state)
    {
        OnLassoStateChanged?.Invoke(state);
    }
}