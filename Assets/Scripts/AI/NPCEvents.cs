using System;
using FishNet.Object;

public static class NPCEvents
{
    public static event Action<NetworkObject> OnDeadEvent;
    
    public static void RaiseDeadEvent(NetworkObject npc) => OnDeadEvent?.Invoke(npc);
}