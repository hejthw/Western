using System;

public static class PlayerEffectsEvents
{
    public static event Action OnWhiskeyUse;
    public static event Action<float> OnSpeedBuff;
    public static event Action<float> OnWalkDebuff;
    public static event Action<float> OnRecoilBuff;

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
    
}