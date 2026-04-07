using System;

public static  class PlayerEvents
{
    public static event Action<float> OnLocalStaminaChange;
    
    public static event Action NextTargetEvent;
    public static event Action PrevTargetEvent;
    public static event Action OnSuspicion;

    public static void RaiseSuspicion()
    {
        OnSuspicion?.Invoke();
    }

    public static void RaiseNextTargetEvent()
    {
        NextTargetEvent?.Invoke();
    }

    public static void RaisePrevTargetEvent()
    {
        PrevTargetEvent?.Invoke();
    }
    
    public static void RaiseStaminaChange(float amount)
    {
        OnLocalStaminaChange?.Invoke(amount);
    }
}