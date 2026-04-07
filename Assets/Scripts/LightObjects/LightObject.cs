using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LightObject : NetworkBehaviour, ILassoInteractable
{
    private Rigidbody rb;

    protected readonly SyncVar<ItemState> state = new SyncVar<ItemState>();
    protected readonly SyncVar<NetworkObject> holder = new SyncVar<NetworkObject>();

    [SerializeField] private bool fragile = false;
    [SerializeField] private float pullSpeed = 15f;
    private bool _isLocallyHeld = false;
    public bool IsLocallyHeld => _isLocallyHeld;

    public void SetLocallyHeld(bool held)
    {
        _isLocallyHeld = held;
        Debug.Log($"[LightObject] SetLocallyHeld({held}) on {name}");
    }
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
        Debug.Log($"[LightObject] ServerPickup called on {name}, state={state.Value}, player={player.name}, owner={player.Owner?.ClientId}");
        if (state.Value != ItemState.World) return;

        holder.Value = player;
        state.Value = ItemState.Held;
        GiveOwnership(player.Owner);

        Debug.Log($"[LightObject] About to call TargetConfirmPickup for connection {player.Owner?.ClientId}");
        TargetConfirmPickup(player.Owner, player);
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

        // Сбросить локальный флаг у предыдущего владельца
        TargetResetLocallyHeld(Owner);

        ObserversApplyThrow(pos, velocity);
    }

    [TargetRpc]
    private void TargetResetLocallyHeld(NetworkConnection target)
    {
        SetLocallyHeld(false);
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
    [TargetRpc]
    public void TargetConfirmPickup(NetworkConnection target, NetworkObject playerObject)
    {
        Debug.Log($"[LightObject] TargetConfirmPickup received on client. target={target?.ClientId}, LocalConnection={LocalConnection?.ClientId}, playerObject={playerObject?.name}");
        if (playerObject == null)
        {
            Debug.Log("[LightObject] playerObject is null");
            return;
        }
        if (playerObject.Owner != LocalConnection)
        {
            Debug.Log($"[LightObject] Owner mismatch: playerObject.Owner={playerObject.Owner?.ClientId}, LocalConnection={LocalConnection?.ClientId}");
            return;
        }

        var pickup = playerObject.GetComponent<PickupController>();
        Debug.Log($"[LightObject] pickup component = {(pickup != null ? "found" : "null")}");
        if (pickup != null)
        {
            pickup.AttachLocal(gameObject);
            SetLocallyHeld(true);
        }
    }
    public virtual byte[] SerializeState()
    {
        return null;
    }

    public virtual void DeserializeState(byte[] data)
    {
        
    }
}