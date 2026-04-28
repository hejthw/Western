using System;

public static class GameLogicEvents
{
    public static Action OnTimerFinished;

    public static void RaiseTimerFinished()
    {
        OnTimerFinished?.Invoke();
    }
}