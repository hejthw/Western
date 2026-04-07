using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LightObject : NetworkBehaviour, ILassoInteractable
{
    private Rigidbody rb;

    private readonly SyncVar<ItemState> state = new SyncVar<ItemState>();
    private readonly SyncVar<NetworkObject> holder = new SyncVar<NetworkObject>();

    [SerializeField] private bool fragile = false;
    [SerializeField] private float pullSpeed = 15f;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public LassoInteractionType GetInteractionType()
    {
        return LassoInteractionType.PullObject;
    }


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
    public void OnLassoAttach(LassoNetwork lasso)
    {
        if (!IsServer) return;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }




    public void OnLassoPull(LassoNetwork lasso)
    {
        if (!IsServer) return;

        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 dir = (lasso.Owner.transform.position - transform.position).normalized;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dir * 20f, ForceMode.Impulse);
    }

    [ObserversRpc]
    private void ObserversMove(Vector3 pos)
    {
        if (IsServer) return;
        transform.position = pos;
    }
    public void OnLassoDetach(LassoNetwork lasso)
    {
        if (!IsServer) return;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}
