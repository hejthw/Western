using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LightObject : NetworkBehaviour, ISavableItem
{
    public bool fragile = false;

    private Rigidbody rb;
    private bool wasThrown = false;

    public readonly SyncVar<NetworkObject> heldBy = new SyncVar<NetworkObject>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        heldBy.OnChange += OnHeldByChanged;
    }

    private void OnHeldByChanged(NetworkObject oldValue, NetworkObject newValue, bool asServer)
    {
        if (newValue == null && IsOwner)
            GetComponentInParent<PickupController>()?.ClearHeldObject();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerPickup(NetworkObject player)
    {
        if (heldBy.Value != null) return;

        heldBy.Value = player;
        GiveOwnership(player.Owner);
        rb.isKinematic = true;
        rb.useGravity = false;

        TargetOnPickup(player.Owner, player);
    }

    [TargetRpc]
    private void TargetOnPickup(NetworkConnection target, NetworkObject player)
    {
        var pickup = player.GetComponent<PickupController>();
        if (pickup != null)
            pickup.PickupItem(gameObject);
    }

    public void OnPickup()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        wasThrown = false;
    }
    public void OnThrow(Vector3 force)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(force, ForceMode.Impulse);
        }
        wasThrown = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 position, Quaternion rotation, Vector3 force)
    {
        if (heldBy.Value == null) return;

        heldBy.Value = null;
        GiveOwnership(null);

        transform.position = position;
        transform.rotation = rotation;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(force, ForceMode.Impulse);

        ObserversThrow(transform.position, transform.rotation, rb.linearVelocity, rb.angularVelocity);
    }

    [ObserversRpc]
    private void ObserversThrow(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (IsOwner) return;
        transform.position = pos;
        transform.rotation = rot;
        rb.linearVelocity = vel;
        rb.angularVelocity = angVel;
        rb.isKinematic = false;
        rb.useGravity = true;
        heldBy.Value = null;
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

    public byte[] SaveState() => new byte[] { (byte)(fragile ? 1 : 0) };

    public void LoadState(byte[] state)
    {
        if (state != null && state.Length > 0)
            fragile = state[0] == 1;
    }
}