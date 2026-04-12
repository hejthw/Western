using System;

public static class SoundBus
{
    public static event Action<SoundID> OnPlay;

    public static void Play(SoundID id) => OnPlay?.Invoke(id);
}