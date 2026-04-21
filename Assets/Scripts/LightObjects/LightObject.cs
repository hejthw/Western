using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;
using FishNet.Component.Transforming;

public class LightObject : NetworkBehaviour, ILassoInteractable
{
    private Rigidbody rb;
    private Collider[] _selfColliders;
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
        _selfColliders = GetComponentsInChildren<Collider>(true);
    }

    private void Start()
    {
        state.OnChange += OnStateChanged;
        UpdateVisibility(state.Value);
        RefreshPlayerCollisionMode(state.Value);
    }

    private void OnDestroy()
    {
        state.OnChange -= OnStateChanged;
    }

    private void OnStateChanged(ItemState oldState, ItemState newState, bool asServer)
    {
        if (asServer) return;
        UpdateVisibility(newState);
        RefreshPlayerCollisionMode(newState);
    }

    [Server]
    public void OnShot()
    {
      
        if (semiFragile)
        {
            PlayImpactSound(SoundID.RevolverImpactSemiFragile);
            Debug.Log($"[LightObject] Destroyed by shot (semi-fragile)");
            Despawn();
            return;
        }

    
        if (fragile)
        {
            PlayImpactSound(SoundID.RevolverImpactFragile);
            Debug.Log($"[LightObject] Destroyed by shot (fragile)");
            Despawn();
            return;
        }

        PlayImpactSound(SoundID.RevolverImpactDefault);
    }


    public SoundID GetRevolverImpactSound()
    {
        if (fragile) return SoundID.RevolverImpactFragile;
        if (semiFragile) return SoundID.RevolverImpactSemiFragile;
        return SoundID.RevolverImpactDefault;
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

    private void RefreshPlayerCollisionMode(ItemState currentState)
    {
        bool allowCollisionWithPlayers = currentState == ItemState.Thrown;
        ApplyPlayerCollisionIgnore(!allowCollisionWithPlayers);
    }

    private void ApplyPlayerCollisionIgnore(bool ignorePlayers)
    {
        if (_selfColliders == null || _selfColliders.Length == 0)
            _selfColliders = GetComponentsInChildren<Collider>(true);

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int p = 0; p < players.Length; p++)
        {
            if (players[p] == null)
                continue;

            Collider[] playerColliders = players[p].GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < _selfColliders.Length; i++)
            {
                Collider selfCollider = _selfColliders[i];
                if (selfCollider == null)
                    continue;

                for (int j = 0; j < playerColliders.Length; j++)
                {
                    Collider playerCollider = playerColliders[j];
                    if (playerCollider == null)
                        continue;

                    Physics.IgnoreCollision(selfCollider, playerCollider, ignorePlayers);
                }
            }
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        return other.GetComponentInParent<PlayerController>() != null
               || other.GetComponentInParent<PlayerHealth>() != null;
    }

    private void IgnoreCollisionWithPlayerCollider(Collider playerCollider, bool ignore)
    {
        if (playerCollider == null)
            return;

        if (_selfColliders == null || _selfColliders.Length == 0)
            _selfColliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < _selfColliders.Length; i++)
        {
            Collider selfCollider = _selfColliders[i];
            if (selfCollider == null)
                continue;

            Physics.IgnoreCollision(selfCollider, playerCollider, ignore);
        }
    }

    public LassoInteractionType GetInteractionType()
    {
        return LassoInteractionType.PullObject;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerPickup(NetworkObject player)
    {
        if (player == null) return;
        if (state.Value != ItemState.World) return;
        if (!CanPickup(player)) return;

        holder.Value = player;
        state.Value = ItemState.Held;
        ApplyHeldPhysics();
        RefreshPlayerCollisionMode(ItemState.Held);
        GiveOwnership(player.Owner);
        SetVisibleForObservers(false);
        bool handled = OnPickup(player);
        Debug.Log($"[LightObject] ServerPickup: OnPickup returned {handled}");
        if (handled) return;

        ShowVisualForObservers(player);
        TargetConfirmPickup(player.Owner, player);
    }

    protected virtual bool OnPickup(NetworkObject player)
    {
        return false;
    }
    
    [Server]
    protected virtual bool CanPickup(NetworkObject player)
    {
        return true;
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
        if (held)
            RefreshPlayerCollisionMode(ItemState.Held);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 pos, Vector3 velocity)
    {
        if (state.Value != ItemState.Held) return;
        SetVisibleForObservers(true);
        NetworkConnection previousHolder = holder.Value != null ? holder.Value.Owner : null;
        
        HideVisualForObservers();
        state.Value = ItemState.Thrown;
        holder.Value = null;
        RemoveOwnership();
        RefreshPlayerCollisionMode(ItemState.Thrown);

        transform.position = pos;
        ApplyWorldPhysics();
        RefreshPlayerCollisionMode(ItemState.Thrown);
        rb.linearVelocity = velocity;

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = true;

        ObserversApplyThrow(pos, velocity);
        if (previousHolder != null)
            TargetResetLocallyHeld(previousHolder);
    }

    [ObserversRpc]
    private void ObserversApplyThrow(Vector3 pos, Vector3 velocity)
    {
        if (IsOwner) return;

        transform.position = pos;
        ApplyWorldPhysics();
        RefreshPlayerCollisionMode(ItemState.Thrown);
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
        if (state.Value != ItemState.Thrown && IsPlayerCollider(collision.collider))
        {
            // Runtime safety-net: if this object touched a player before state sync/spawn timing settled,
            // permanently ignore that pair while the object is not in thrown mode.
            IgnoreCollisionWithPlayerCollider(collision.collider, true);
            return;
        }

        if (!IsServer) return;
        if (state.Value != ItemState.Thrown) return;

   
        if (collision.collider.TryGetComponent(out PlayerHealth playerHealth))
        {
            Vector3 hitDirection = collision.transform.position - transform.position;
            hitDirection.y = 0.5f;
            hitDirection.Normalize();

            if (fragile)
            {

                PlayImpactSound(SoundID.RevolverImpactFragile);
                HideVisualForObservers();
                Despawn();
            }
            else
            {
                PlayImpactSound(SoundID.RevolverImpactDefault);
                HideVisualForObservers();
                SetVisibleForObservers(true);
                state.Value = ItemState.World;
                var nt = GetComponent<NetworkTransform>();
                if (nt != null) nt.enabled = true;
                if (rb != null) rb.linearVelocity = Vector3.zero;
                ApplyWorldPhysics();
                RefreshPlayerCollisionMode(ItemState.World);
            }
            
            OnHitPlayer(playerHealth);
            
            return;
        }


        if (fragile)
        {
 
            PlayImpactSound(SoundID.RevolverImpactFragile);
            HideVisualForObservers();
            Despawn();
            return;
        }

        PlayImpactSound(SoundID.RevolverImpactDefault);
        HideVisualForObservers();
        SetVisibleForObservers(true);
        state.Value = ItemState.World;
        var ntWorld = GetComponent<NetworkTransform>();
        if (ntWorld != null) ntWorld.enabled = true;
        ApplyWorldPhysics();
        RefreshPlayerCollisionMode(ItemState.World);
    }
    
    protected virtual void OnHitPlayer(PlayerHealth playerHealth) { }
    
    [Server]
    public void ForceDrop()
    {
        NetworkConnection previousHolder = holder.Value != null ? holder.Value.Owner : null;
        HideVisualForObservers();
        SetVisibleForObservers(true);
        holder.Value = null;
        state.Value = ItemState.World;
        RemoveOwnership();
        ApplyWorldPhysics();
        RefreshPlayerCollisionMode(ItemState.World);

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = true;

        if (previousHolder != null)
            TargetResetLocallyHeld(previousHolder);
    }

    public void OnLassoAttach(LassoNetwork lasso)
    {
        if (!IsServer) return;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        OnLassoPull(lasso);
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
        lasso.ReturnToPlayer();
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
    
    private void ApplyHeldPhysics()
    {
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    private void ApplyWorldPhysics()
    {
        if (rb == null) return;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
    
    [Server]
    public void EquipToPlayer(NetworkObject player)
    {
        if (player == null) return;
        if (state.Value == ItemState.Held) return;
        holder.Value = player;
        state.Value = ItemState.Held;
        ApplyHeldPhysics();
        RefreshPlayerCollisionMode(ItemState.Held);
        GiveOwnership(player.Owner);
        SetVisibleForObservers(false);
        ShowVisualForObservers(player);
        bool handled = OnPickup(player);
        if (handled) return;
        TargetConfirmPickup(player.Owner, player);
    }

    [ObserversRpc]
    public void ShowVisualForObservers(NetworkObject holderNetObj)
    {
        if (IsOwner) return;
        if (_currentVisual != null) Destroy(_currentVisual);
        if (holderNetObj == null) return;
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
    private void PlayImpactSound(SoundID soundId)
    {
        SoundBus.Play(soundId);
    }
    [ObserversRpc]
    public void SetVisibleForObservers(bool visible)
    {
        if (IsOwner) return; 
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = visible;
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = visible;
    }

}