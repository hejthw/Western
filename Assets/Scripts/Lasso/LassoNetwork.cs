using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LassoNetwork : NetworkBehaviour
{
    [Header("Settings")]
    public float throwSpeed = 40f;
    public float returnSpeed = 25f;
    public float maxDistance = 50f;
    public float playerPullSpeed = 15f;

    private LassoController controller;

    public readonly SyncVar<NetworkObject> attachedNetObj = new();

    private ILassoInteractable currentInteractable;

    private bool isFlying;
    private bool isReturning;

    private bool isPullingPlayer;
    private Vector3 pullTarget;
    private Vector3 hitPoint;
    private Vector3 moveDir;
    private NetworkObject ownerNetObj;
    public bool CanThrow => !isFlying && !isReturning && attachedNetObj.Value == null;
    public GameObject Owner => ownerNetObj != null ? ownerNetObj.gameObject : null;
    public NetworkObject AttachedObject => attachedNetObj.Value;

    public Vector3 HitPoint => hitPoint;

    private void Awake()
    {
        controller = GetComponentInParent<LassoController>();
    }

    // ================= THROW =================

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 startPos, Vector3 direction, NetworkObject owner)
    {
        Debug.Log("[SERVER] Throw called");

        if (!CanThrow)
        {
            Debug.Log("[SERVER] Cannot throw");
            return;
        }

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

    // ================= UPDATE =================

    private void Update()
    {
        if (!IsServer) return;

        if (isFlying)
            MoveForward();

        if (isReturning)
            MoveBack();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (ownerNetObj == null || !isPullingPlayer) return;

        Vector3 dir = pullTarget - Owner.transform.position;

        // Останавливаем притягивание, когда подошли близко
        if (dir.magnitude < 1.5f)
        {
            Debug.Log("[SERVER] Reached target - stopping pull");
            StopPullPlayer();
            ReturnToPlayer();
            return;
        }

        // Сервер форсирует скорость
        if (Owner.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = dir.normalized * playerPullSpeed;
        }
        else
        {
            Debug.LogError("[SERVER] Player has no Rigidbody!");
        }
    }
    // ================= MOVE =================

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

    // ================= HIT =================

    private void HandleHit(RaycastHit hit)
    {
        Collider col = hit.collider;

        hitPoint = hit.point;

        if (col.TryGetComponent(out NetworkObject netObj))
        {
            attachedNetObj.Value = netObj;
            isFlying = false;

            currentInteractable = netObj.GetComponent<ILassoInteractable>();
            currentInteractable?.OnLassoAttach(this);

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

    // ================= INPUT =================

    [ServerRpc(RequireOwnership = false)]
    public void ServerPullPressed()
    {
        Debug.Log("[SERVER] PullPressed called");

        if (attachedNetObj.Value == null)
        {
            Debug.Log("[SERVER] No attached object");
            return;
        }

        if (!attachedNetObj.Value.TryGetComponent(out ILassoInteractable interactable))
        {
            Debug.Log("[SERVER] No interactable");
            return;
        }

        var type = interactable.GetInteractionType();

        Debug.Log($"[SERVER] Interaction type: {type}");

        switch (type)
        {
            case LassoInteractionType.PullObject:
                Debug.Log("[SERVER] PullObject");

                interactable.OnLassoPull(this);
                interactable.OnLassoDetach(this);

                RpcPullObject();
                ReturnToPlayer();
                break;

            case LassoInteractionType.PullPlayer:
                Debug.Log("[SERVER] PullPlayer");

                // 🔥 ВАЖНО: задаем точку притяжения
                pullTarget = transform.position;

                interactable.OnLassoPull(this);

                RpcStartPullPlayer(pullTarget);
                break;
        }
    }

    [ObserversRpc]
    private void RpcPullObject()
    {
        // чисто визуал если нужно
    }

    [ObserversRpc]
    private void RpcStartPullPlayer(Vector3 target)
    {
        Debug.Log("[SERVER] StartPullingPlayer");
        if (IsServer) return;

        isPullingPlayer = true;
        pullTarget = target;
    }

    // ================= PLAYER PULL =================

    [Server]
    public void StartPullingPlayer(Vector3 point)
    {
        if (Owner.TryGetComponent(out PlayerController pc))
        {
            pc.SetLassoState(true);
        }
        isPullingPlayer = true;
        pullTarget = point;

        RpcStartPullPlayer(point);
    }

    private void StopPullPlayer()
    {
        if (Owner.TryGetComponent(out PlayerController pc))
        {
            pc.SetLassoState(false);
        }
        isPullingPlayer = false;
    }

    // ================= RETURN =================

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
        currentInteractable?.OnLassoDetach(this);

        StopPullPlayer();

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

    // ================= UTILS =================

    private void IgnorePlayerCollision(bool ignore)
    {
        var lassoCol = GetComponent<Collider>();
        var playerCol = controller.GetComponent<Collider>();

        if (lassoCol && playerCol)
            Physics.IgnoreCollision(lassoCol, playerCol, ignore);
    }
}