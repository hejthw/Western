using UnityEngine;
using FishNet.Object;

public class LightObject : NetworkBehaviour
{
    public bool fragile = false;

    private Rigidbody rb;
    private bool wasThrown = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void OnPickup()
    {
        if (rb != null) rb.isKinematic = true;
        wasThrown = false;
    }

    public void OnThrow(Vector3 throwForce)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(throwForce, ForceMode.Impulse);
        }
        wasThrown = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!wasThrown || !fragile) return;

       
        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
        else
            DespawnServerRpc();
    }

    [ServerRpc]
    private void DespawnServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}