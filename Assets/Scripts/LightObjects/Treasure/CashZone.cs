using UnityEngine;
using FishNet.Object;

public class CashZone : NetworkBehaviour
{
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var pickup = other.GetComponentInParent<PickupController>();
        if (pickup == null) return;

        Debug.Log("Player entered cash zone");
        UIEvents.RaiseOnCashZoneChanged(true);
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        UIEvents.RaiseOnCashZoneChanged(false);
    }
}