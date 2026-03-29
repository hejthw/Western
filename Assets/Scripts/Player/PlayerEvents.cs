using System;

public static  class PlayerEvents
{
    public static event Action<int> OnLocalHealthChange;
    
    public static event Action<float> OnLocalStaminaChange;
    
    public static event Action<bool> OnDeadEvent;
    
    public static event Action<bool> OnKnockoutEvent;
    public static event Action NextTargetEvent;
    public static event Action PrevTargetEvent;

    public static void RaiseNextTargetEvent()
    {
        NextTargetEvent?.Invoke();
    }

    public static void RaisePrevTargetEvent()
    {
        PrevTargetEvent?.Invoke();
    }
    
    public static void RaiseHealthChange(int amount)
    {
        OnLocalHealthChange?.Invoke(amount);
    }

    public static void RaiseStaminaChange(float amount)
    {
        OnLocalStaminaChange?.Invoke(amount);
    }

    public static void RaiseDeadEvent(bool isDead)
    {
        OnDeadEvent?.Invoke(isDead);
    }

    public static void RaiseKnockoutEvent(bool isKnockout)
    {
        OnKnockoutEvent?.Invoke(isKnockout);
    }
}