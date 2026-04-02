using System;

public static class NPCEvents
{
    public static event Action OnDeadEvent;
    
    public static void RaiseDeadEvent() => OnDeadEvent?.Invoke();
}