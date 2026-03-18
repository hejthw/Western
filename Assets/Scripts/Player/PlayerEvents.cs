using System;

public static  class PlayerEvents
{
    public static event Action<int> OnLocalHealthChange;
    
    public static void RaiseHealthChange(int amount)
    {
        OnLocalHealthChange?.Invoke(amount);
    }
}