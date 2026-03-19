using System;

public static  class PlayerEvents
{
    public static event Action<int> OnLocalHealthChange;
    
    public static event Action<float> OnLocalStaminaChange;
    
    public static void RaiseHealthChange(int amount)
    {
        OnLocalHealthChange?.Invoke(amount);
    }

    public static void RaiseStaminaChange(float amount)
    {
        OnLocalStaminaChange?.Invoke(amount);
    }
}