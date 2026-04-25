using FishNet.Connection;
using UnityEngine;
using FishNet.Object;

public class CashZone : NetworkBehaviour
{
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var pickup = other.GetComponentInParent<PickupController>();
        if (pickup == null) return;
        
        var playerNetObj = pickup.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        Debug.Log("Player entered cash zone");
        pickup.TargetSetCashZone(playerNetObj.Owner, true);
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        var pickup = other.GetComponentInParent<PickupController>();
        if (pickup == null) return;

        var playerNetObj = pickup.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        pickup.TargetSetCashZone(playerNetObj.Owner, false);
    }
}