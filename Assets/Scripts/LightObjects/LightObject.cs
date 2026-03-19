using UnityEngine;
using FishNet.Object;

public class LightObject : NetworkBehaviour
{
    
    public bool fragile = false;          

    private Rigidbody rb;
    private NetworkObject netObj;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        netObj = GetComponent<NetworkObject>();
    }

   
    public void OnPickup()
    {
        if (rb != null) rb.isKinematic = true;
    }

   
    public void OnThrow(Vector3 throwForce)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(throwForce, ForceMode.Impulse);
        }

        if (fragile)
        {
            DespawnObject();
        }
    }

    private void DespawnObject()
    {
        if (IsServer)
            netObj.Despawn();
        else
            DespawnServerRpc();
    }

    [ServerRpc]
    private void DespawnServerRpc()
    {
        if (netObj != null)
            netObj.Despawn();
    }
}