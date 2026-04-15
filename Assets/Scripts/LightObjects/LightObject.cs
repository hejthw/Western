using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;
using FishNet.Component.Transforming;

public class LightObject : NetworkBehaviour, ILassoInteractable
{
    private Rigidbody rb;
    protected readonly SyncVar<ItemState> state = new SyncVar<ItemState>();
    protected readonly SyncVar<NetworkObject> holder = new SyncVar<NetworkObject>();

    [SerializeField] private bool fragile = false;
    [SerializeField] private float pullSpeed = 15f;
    [SerializeField] private bool semiFragile = false;
    [SerializeField] private GameObject thirdPersonVisualPrefab;

    private bool _isLocallyHeld = false;
    public bool IsLocallyHeld => _isLocallyHeld;
    private GameObject _currentVisual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        state.OnChange += OnStateChanged;
        UpdateVisibility(state.Value);
    }

    private void OnDestroy()
    {
        state.OnChange -= OnStateChanged;
    }

    private void OnStateChanged(ItemState oldState, ItemState newState, bool asServer)
    {
        if (asServer) return;
        UpdateVisibility(newState);
    }

    [Server]
    public void OnShot()
    {
        if (!semiFragile) return;
        if (state.Value == ItemState.Held) return; 

        Debug.Log($"[LightObject] Destroyed by shot (semi-fragile)");
        Despawn();
    }

    private void UpdateVisibility(ItemState currentState)
    {
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            bool shouldBeEnabled = (currentState != ItemState.Held);
            if (netTransform.enabled != shouldBeEnabled)
                netTransform.enabled = shouldBeEnabled;
        }
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
        ShowVisualForObservers(player);
        bool handled = OnPickup(player);
        Debug.Log($"[LightObject] ServerPickup: OnPickup returned {handled}");
        if (handled) return;

        TargetConfirmPickup(player.Owner, player);
    }

    protected virtual bool OnPickup(NetworkObject player)
    {
        return false;
    }

    [TargetRpc]
    public void TargetConfirmPickup(NetworkConnection target, NetworkObject playerObject)
    {
        if (playerObject == null) return;
        if (playerObject.Owner != LocalConnection) return;

        var pickup = playerObject.GetComponent<PickupController>();
        if (pickup != null)
        {
            pickup.AttachLocal(gameObject);
            SetLocallyHeld(true);
        }
    }

    public void SetLocallyHeld(bool held)
    {
        _isLocallyHeld = held;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 pos, Vector3 velocity)
    {
        if (state.Value != ItemState.Held) return;
        HideVisualForObservers();
        state.Value = ItemState.Thrown;
        holder.Value = null;
        RemoveOwnership();

        transform.position = pos;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = true;

        ObserversApplyThrow(pos, velocity);
        TargetResetLocallyHeld(Owner);
    }

    [ObserversRpc]
    private void ObserversApplyThrow(Vector3 pos, Vector3 velocity)
    {
        if (IsOwner) return;

        transform.position = pos;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = true;
    }

    [TargetRpc]
    private void TargetResetLocallyHeld(NetworkConnection target)
    {
        SetLocallyHeld(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (state.Value != ItemState.Thrown) return;

  
        if (collision.collider.TryGetComponent(out PlayerHealth playerHealth))
        {

            Vector3 hitDirection = collision.transform.position - transform.position;
            hitDirection.y = 0.5f; 
            hitDirection.Normalize();

            //playerHealth.StunWithDirection(Vector3.zero, 2f);

            if (fragile)
            {
                Despawn();
            }
            else
            {
                HideVisualForObservers();
                state.Value = ItemState.World;
                var nt = GetComponent<NetworkTransform>();
                if (nt != null) nt.enabled = true;
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
            return;
        }

        if (fragile)
        {
            Despawn();
            return;
        }
        HideVisualForObservers();
        state.Value = ItemState.World;
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null) netTransform.enabled = true;
    }

    [Server]
    public void ForceDrop()
    {
        HideVisualForObservers();
        holder.Value = null;
        state.Value = ItemState.World;

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = true;

        TargetResetLocallyHeld(Owner);
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
        var playerNetObj = lasso.OwnerNetObj;
        if (playerNetObj == null) return;

        var playerObj = playerNetObj.gameObject;
        if (playerObj == null) return;

        Vector3 dir = (playerObj.transform.position - transform.position).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dir * 20f, ForceMode.Impulse);
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

    public virtual byte[] SerializeState()
    {
        return null;
    }

    public virtual void DeserializeState(byte[] data) 
    {
    }
    public void SetFragile(bool value)
    {
        fragile = value;
    }

    public void SetSemiFragile(bool value)
    {
        semiFragile = value;
    }
    [Server]
    public void EquipToPlayer(NetworkObject player)
    {
        if (player == null) return;
        if (state.Value == ItemState.Held) return;
        holder.Value = player;
        state.Value = ItemState.Held;
        GiveOwnership(player.Owner);
        bool handled = OnPickup(player);
        if (handled) return;
        TargetConfirmPickup(player.Owner, player);
    }

    [ObserversRpc]
    public void ShowVisualForObservers(NetworkObject holderNetObj)
    {
        if (IsOwner) return;
        if (_currentVisual != null) Destroy(_currentVisual);
        if (thirdPersonVisualPrefab == null)
        {
            Debug.LogWarning($"{name} has no thirdPersonVisualPrefab assigned!");
            return;
        }
        var controller = holderNetObj.GetComponent<PlayerController>();
        if (controller == null || controller.weaponHoldPoint == null)
        {
            Debug.LogError("Cannot attach visual: PlayerController or weaponHoldPoint missing");
            return;
        }
        _currentVisual = Instantiate(thirdPersonVisualPrefab, controller.weaponHoldPoint);
        _currentVisual.transform.localPosition = Vector3.zero;
        _currentVisual.transform.localRotation = Quaternion.identity;
    }

    [ObserversRpc]
    public void HideVisualForObservers()
    {
        if (IsOwner) return;
        if (_currentVisual != null)
        {
            Destroy(_currentVisual);
            _currentVisual = null;
        }
    }
}