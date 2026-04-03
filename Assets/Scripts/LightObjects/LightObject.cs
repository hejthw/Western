using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LightObject : NetworkBehaviour
{
    private Rigidbody rb;

    private readonly SyncVar<ItemState> state = new SyncVar<ItemState>();
    private readonly SyncVar<NetworkObject> holder = new SyncVar<NetworkObject>();

    [SerializeField] private bool fragile = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // =========================
    // SERVER LOGIC
    // =========================

    [ServerRpc(RequireOwnership = false)]
    public void ServerPickup(NetworkObject player)
    {
        if (state.Value != ItemState.World) return;

        holder.Value = player;
        state.Value = ItemState.Held;

        GiveOwnership(player.Owner);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 pos, Vector3 velocity)
    {
        if (state.Value != ItemState.Held) return;

        state.Value = ItemState.Thrown;
        holder.Value = null;

        RemoveOwnership();

        transform.position = pos;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;

        ObserversApplyThrow(pos, velocity);
    }

    [ObserversRpc]
    private void ObserversApplyThrow(Vector3 pos, Vector3 velocity)
    {
        if (IsOwner) return;

        transform.position = pos;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;
    }

    // =========================
    // COLLISION
    // =========================

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (state.Value != ItemState.Thrown) return;

        if (fragile)
        {
            Despawn();
            return;
        }

        state.Value = ItemState.World;
    }

    [Server]
    public void ForceDrop()
    {
        holder.Value = null;
        state.Value = ItemState.World;
    }
}