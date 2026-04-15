using System;

public static class PlayerEffectsEvents
{
    public static event Action OnWhiskeyUse;
    public static event Action<float> OnSpeedBuff;
    public static event Action<float> OnWalkDebuff;
    public static event Action<float> OnRecoilBuff;
    public static event Action OnThrowup;

    public static void RaiseWhiskeyUse()
    {
        OnWhiskeyUse?.Invoke();
    }

    public static void RaiseSpeedBuff(float value)
    {
        OnSpeedBuff?.Invoke(value);
    }

    public static void RaiseRecoilBuff(float value)
    {
        OnRecoilBuff?.Invoke(value);
    }
    
    public static void RaiseWalkDebuff(float value)
    {
        OnWalkDebuff?.Invoke(value);
    }

    public static void RaiseThrowup()
    {
        OnThrowup?.Invoke();
    }
    
    public static event Action OnDrunkExpired;
    public static void RaiseDrunkExpired() => OnDrunkExpired?.Invoke();
}