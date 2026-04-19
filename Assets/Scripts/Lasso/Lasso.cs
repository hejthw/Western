using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;
using FishNet.Connection;

public class LassoNetwork : NetworkBehaviour
{
    [Header("Settings")]
    public float throwSpeed = 40f;
    public float returnSpeed = 25f;
    public float maxDistance = 50f;
    public float playerPullSpeed = 15f;
    public float pullAcceleration = 45f;
    public float pullStopDistance = 1.4f;
    public float finishBackoffDistance = 0.35f;
    public float finishSettleTime = 0.08f;
    public float jumpOffForce = 7f;
    public float jumpOffForwardFactor = 0.55f;

    private LassoController controller;

    public readonly SyncVar<NetworkObject> attachedNetObj = new();

    private ILassoInteractable currentInteractable;

    private bool isFlying;
    private bool isReturning;
    private Vector3 hitPoint;
    private Vector3 climbTargetPoint;
    private Vector3 moveDir;
    private NetworkObject ownerNetObj;
    private PlayerController cachedPlayerController;
    private Coroutine serverPullCoroutine;
    private Rigidbody pullingPlayerRb;
    private bool isPlayerPulling;
    private bool isPullInputHeld;
    private float currentPullSpeed;
    private bool _isFinishingPull;

    public bool CanThrow => !isFlying && !isReturning && attachedNetObj.Value == null;
    public GameObject Owner => ownerNetObj != null ? ownerNetObj.gameObject : null;
    public NetworkObject AttachedObject => attachedNetObj.Value;
    public Vector3 HitPoint => hitPoint;
    public NetworkObject OwnerNetObj => ownerNetObj;
    public bool IsPlayerPulling => isPlayerPulling;

    private void Awake()
    {
        controller = GetComponentInParent<LassoController>();
    }

    private PlayerController GetPlayerController()
    {
        if (cachedPlayerController != null)
            return cachedPlayerController;
        cachedPlayerController = GetComponentInParent<PlayerController>();
        if (cachedPlayerController != null)
        {
            Debug.Log("[LassoNetwork] Found PlayerController in parent");
            return cachedPlayerController;
        }
        if (controller != null)
        {
            cachedPlayerController = controller.GetComponent<PlayerController>();
            if (cachedPlayerController != null)
            {
                Debug.Log("[LassoNetwork] Found PlayerController via LassoController");
                return cachedPlayerController;
            }
            cachedPlayerController = controller.GetComponentInParent<PlayerController>();
            if (cachedPlayerController != null)
            {
                Debug.Log("[LassoNetwork] Found PlayerController in parent of LassoController");
                return cachedPlayerController;
            }
        }

        Debug.LogError("[LassoNetwork] PlayerController not found! Check object hierarchy.");
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 startPos, Vector3 direction, NetworkObject owner)
    {
        if (!CanThrow)
            return;

        ownerNetObj = owner;

        DoThrow(startPos, direction);
        RpcThrow(startPos, direction);
    }

    private void DoThrow(Vector3 startPos, Vector3 direction)
    {
        transform.SetParent(null);
        transform.position = startPos;
        transform.forward = direction;

        gameObject.SetActive(true);

        IgnorePlayerCollision(true);

        isFlying = true;
        isReturning = false;
        isPlayerPulling = false;
        isPullInputHeld = false;
        currentPullSpeed = 0f;
        _isFinishingPull = false;
        attachedNetObj.Value = null;
        currentInteractable = null;

        moveDir = direction.normalized;
    }

    [ObserversRpc]
    private void RpcThrow(Vector3 startPos, Vector3 direction)
    {
        if (IsServer) return;
        DoThrow(startPos, direction);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isFlying)
            MoveForward();

        if (isReturning)
            MoveBack();
    }

    private void MoveForward()
    {
        float dist = throwSpeed * Time.deltaTime;

        if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, dist))
        {
            transform.position = hit.point;
            HandleHit(hit);
            return;
        }

        transform.position += moveDir * dist;

        if (Vector3.Distance(transform.position, controller.transform.position) > maxDistance)
        {
            StartReturn();
        }
    }

    private void MoveBack()
    {
        Vector3 dir = controller.transform.position - transform.position;
        transform.position += dir.normalized * returnSpeed * Time.deltaTime;

        if (dir.magnitude < 1.5f)
            ReturnToPlayer();
    }

    private void HandleHit(RaycastHit hit)
    {
        hitPoint = hit.point;
        Collider col = hit.collider;

        NetworkObject netObj = col.GetComponentInParent<NetworkObject>();
        if (netObj != null)
        {
            ILassoInteractable interactable = netObj.GetComponent<ILassoInteractable>();
            if (interactable == null)
            {
                StartReturn();
                return;
            }

            if (interactable.GetInteractionType() == LassoInteractionType.PullPlayer)
            {
                UnMovable unMovable = netObj.GetComponent<UnMovable>();
                if (unMovable == null || ownerNetObj == null || !unMovable.CanAttach(ownerNetObj))
                {
                    StartReturn();
                    return;
                }

                if (!unMovable.TryActivateRopeFromLassoHit())
                {
                    StartReturn();
                    return;
                }

                SoundBus.Play(SoundID.LightObjectCaptured);
                StartReturn();
                return;
            }
            else
            {
                climbTargetPoint = hitPoint;
            }

            attachedNetObj.Value = netObj;
            isFlying = false;
            currentInteractable = interactable;
            currentInteractable.OnLassoAttach(this);
            SoundBus.Play(SoundID.LightObjectCaptured);

            RpcAttach(netObj);
            controller.OnLassoAttachedServer(netObj.gameObject);
        }
        else
        {
            StartReturn();
        }
    }

    [ObserversRpc]
    private void RpcAttach(NetworkObject obj)
    {
        if (IsServer) return;

        attachedNetObj.Value = obj;
        isFlying = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerSetPullHeld(bool held)
    {
        if (attachedNetObj.Value == null) return;
        if (!attachedNetObj.Value.TryGetComponent(out ILassoInteractable interactable))
            return;
        if (interactable.GetInteractionType() != LassoInteractionType.PullPlayer)
            return;
        
        isPullInputHeld = held;
        if (held && !isPlayerPulling)
            StartPlayerPullServer();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerPullPressed()
    {
        if (attachedNetObj.Value == null) return;

        if (!attachedNetObj.Value.TryGetComponent(out ILassoInteractable interactable))
            return;

        switch (interactable.GetInteractionType())
        {
            case LassoInteractionType.PullObject:
                interactable.OnLassoPull(this);
                interactable.OnLassoDetach(this);
                RpcPullObject();
                ReturnToPlayer();
                break;

            case LassoInteractionType.PullPlayer:
                isPullInputHeld = true;
                StartPlayerPullServer();
                break;
            case LassoInteractionType.PullCharacter:
                Debug.Log("[SERVER] PullCharacter — выполняем рывок");
                interactable.OnLassoPull(this);
                interactable.OnLassoDetach(this);
                break;
        }
    }

    [Server]
    private void StartPlayerPullServer()
    {
        if (isPlayerPulling) return;
        if (ownerNetObj == null) return;

        PlayerController playerController = ownerNetObj.GetComponent<PlayerController>();
        if (playerController == null) return;

        pullingPlayerRb = ownerNetObj.GetComponent<Rigidbody>();
        if (pullingPlayerRb == null) return;

        isPlayerPulling = true;
        playerController.ServerSetForcedMoveNetworkMode(true);
        ApplyPulledStateLocal(ownerNetObj, true);
        RpcSetPulledState(ownerNetObj.ObjectId, true);
        currentInteractable?.OnLassoPull(this);
        currentPullSpeed = 0f;

        TargetSetPlayerPullState(ownerNetObj.Owner, true);
        if (serverPullCoroutine != null)
            StopCoroutine(serverPullCoroutine);
        serverPullCoroutine = StartCoroutine(ServerPullPlayerCoroutine(ownerNetObj));
    }

    [Server]
    private IEnumerator ServerPullPlayerCoroutine(NetworkObject playerNetObj)
    {
        Transform playerTransform = playerNetObj.transform;

        while (isPlayerPulling && attachedNetObj.Value != null)
        {
            float targetSpeed = isPullInputHeld ? playerPullSpeed : 0f;
            currentPullSpeed = Mathf.MoveTowards(
                currentPullSpeed,
                targetSpeed,
                pullAcceleration * Time.fixedDeltaTime
            );

            Vector3 currentPos = playerTransform.position;
            Vector3 toTarget = climbTargetPoint - currentPos;
            float distance = toTarget.magnitude;

            if (distance <= pullStopDistance)
            {
                Vector3 safeEndPos = currentPos;
                if (distance > 0.001f)
                {
                    float finishStep = Mathf.Max(0f, distance - Mathf.Min(finishBackoffDistance, pullStopDistance));
                    safeEndPos = currentPos + toTarget.normalized * finishStep;
                }

                pullingPlayerRb.MovePosition(safeEndPos);
                if (!_isFinishingPull)
                    StartCoroutine(FinishPullAfterSettle(safeEndPos, playerNetObj.Owner));
                yield break;
            }

            if (currentPullSpeed <= 0.001f)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            Vector3 step = toTarget.normalized * (currentPullSpeed * Time.fixedDeltaTime);
            if (step.magnitude > distance)
                step = toTarget;
            Vector3 nextPos = currentPos + step;

            pullingPlayerRb.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
        }
    }

    [Server]
    private IEnumerator FinishPullAfterSettle(Vector3 settlePosition, NetworkConnection ownerConnection)
    {
        _isFinishingPull = true;
        float t = 0f;
        while (t < finishSettleTime)
        {
            if (pullingPlayerRb == null)
                yield break;
            pullingPlayerRb.MovePosition(settlePosition);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        ReturnToPlayer();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerJumpOffPull()
    {
        if (!isPlayerPulling) return;
        if (ownerNetObj == null) return;
        if (pullingPlayerRb == null) return;

        Vector3 jumpDirection = (Vector3.up + ownerNetObj.transform.forward * jumpOffForwardFactor).normalized;
        ApplyPulledStateLocal(ownerNetObj, false);
        RpcSetPulledState(ownerNetObj.ObjectId, false);
        pullingPlayerRb.linearVelocity = Vector3.zero;
        pullingPlayerRb.AddForce(jumpDirection * jumpOffForce, ForceMode.Impulse);
        RpcApplyJumpOff(ownerNetObj.ObjectId, jumpDirection * jumpOffForce);
        ReturnToPlayer();
    }

    [ObserversRpc]
    private void RpcApplyJumpOff(int playerObjectId, Vector3 impulse)
    {
        if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(playerObjectId, out NetworkObject playerObj))
            return;

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    [TargetRpc]
    private void TargetSetPlayerPullState(NetworkConnection connection, bool active)
    {
        PlayerController pc = GetPlayerController();
        if (pc == null) return;
        
        Rigidbody rb = pc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (active)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (active)
        {
            pc.DisableMovement();
            pc.SetLassoState(true);
        }
        else
        {
            pc.EnableMovement();
            PlayerPhysics ph = pc.GetComponent<PlayerPhysics>();
            ph?.ResetOwnerMovementPredictionAfterForcedMove();
            pc.SetLassoState(false);
        }
    }

    [ObserversRpc]
    private void RpcPullObject() { }

    [ServerRpc(RequireOwnership = false)]
    public void ServerDetachAndReturn()
    {
        ReturnToPlayer();
    }

    private void StartReturn()
    {
        isFlying = false;
        isReturning = true;
        RpcStartReturn();
    }

    [ObserversRpc]
    private void RpcStartReturn()
    {
        if (IsServer) return;
        isFlying = false;
        isReturning = true;
    }

    private void ReturnToPlayer()
    {
        if (serverPullCoroutine != null)
        {
            StopCoroutine(serverPullCoroutine);
            serverPullCoroutine = null;
        }

        if (isPlayerPulling && ownerNetObj != null)
        {
            PlayerController ownerPc = ownerNetObj.GetComponent<PlayerController>();
            ownerPc?.ServerSetForcedMoveNetworkMode(false);
            ownerPc?.ServerBroadcastPostForcedMoveResync();
            TargetSetPlayerPullState(ownerNetObj.Owner, false);
        }

        if (ownerNetObj != null)
        {
            ApplyPulledStateLocal(ownerNetObj, false);
            RpcSetPulledState(ownerNetObj.ObjectId, false);
        }

        isPlayerPulling = false;
        isPullInputHeld = false;
        currentPullSpeed = 0f;
        _isFinishingPull = false;
        pullingPlayerRb = null;

        currentInteractable?.OnLassoDetach(this);
        currentInteractable = null;
        attachedNetObj.Value = null;
        isFlying = false;
        isReturning = false;

        transform.SetParent(controller.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(false);
        IgnorePlayerCollision(false);

        RpcReturn();
        controller.OnLassoReturnedServer();
    }

    [ObserversRpc]
    private void RpcReturn()
    {
        if (IsServer) return;

        transform.SetParent(controller.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(false);
    }

    private void IgnorePlayerCollision(bool ignore)
    {
        var lassoCol = GetComponent<Collider>();
        var playerCol = controller.GetComponent<Collider>();

        if (lassoCol && playerCol)
            Physics.IgnoreCollision(lassoCol, playerCol, ignore);
    }
    
    private void ApplyPulledStateLocal(NetworkObject playerObj, bool active)
    {
        if (playerObj == null) return;

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (active)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    [ObserversRpc]
    private void RpcSetPulledState(int playerObjectId, bool active)
    {
        if (IsServerInitialized) return;
        if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(playerObjectId, out NetworkObject playerObj))
            return;

        ApplyPulledStateLocal(playerObj, active);
    }
}