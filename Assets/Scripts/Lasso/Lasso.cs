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

    private LassoController controller;

    public readonly SyncVar<NetworkObject> attachedNetObj = new();

    private ILassoInteractable currentInteractable;

    private bool isFlying;
    private bool isReturning;
    private Coroutine clientPullCoroutine;
    private bool isClientPulling;
    private Vector3 hitPoint;
    private Vector3 moveDir;
    private NetworkObject ownerNetObj;
    private PlayerController cachedPlayerController;

    public bool CanThrow => !isFlying && !isReturning && attachedNetObj.Value == null;
    public GameObject Owner => ownerNetObj != null ? ownerNetObj.gameObject : null;
    public NetworkObject AttachedObject => attachedNetObj.Value;
    public Vector3 HitPoint => hitPoint;
    public NetworkObject OwnerNetObj => ownerNetObj;
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
        Collider col = hit.collider;

        hitPoint = hit.point;

        if (col.TryGetComponent(out NetworkObject netObj))
        {
            attachedNetObj.Value = netObj;
            isFlying = false;

            currentInteractable = netObj.GetComponent<ILassoInteractable>();
            currentInteractable?.OnLassoAttach(this);
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
                Debug.Log("[SERVER] PullPlayer - sending TargetRpc to client");
                if (ownerNetObj != null)
                {
                    TargetStartPullPlayer(base.Owner, ownerNetObj, hitPoint);
                }
                else
                {
                    Debug.LogError("[SERVER] ownerNetObj is null!");
                }
                break;
            case LassoInteractionType.PullCharacter:
                Debug.Log("[SERVER] PullCharacter — выполняем рывок");
                interactable.OnLassoPull(this);
                // Лассо НЕ возвращается сразу — остаётся прикреплённым
                // Отцепление произойдёт автоматически через HoldRoutine или по таймеру
                break;
        }
    }

    [TargetRpc]
    private void TargetStartPullPlayer(NetworkConnection connection, NetworkObject targetPlayer, Vector3 targetPoint)
    {
        if (!IsOwner) return;
        Debug.Log($"[CLIENT] Starting pull towards {targetPoint}");
        if (clientPullCoroutine != null)
            StopCoroutine(clientPullCoroutine);

        clientPullCoroutine = StartCoroutine(PullPlayerCoroutine(targetPoint));
    }

    private IEnumerator PullPlayerCoroutine(Vector3 targetPoint)
    {
        isClientPulling = true;

        PlayerController pc = GetPlayerController();
        if (pc == null)
        {
            Debug.LogError("[CLIENT] No PlayerController found!");
            isClientPulling = false;
            clientPullCoroutine = null;
            yield break;
        }

        pc.DisableMovement();
        pc.SetLassoState(true);

        Transform playerTransform = pc.transform;
        float speed = playerPullSpeed;

        while (Vector3.Distance(playerTransform.position, targetPoint) > 1.5f)
        {
            Vector3 dir = (targetPoint - playerTransform.position).normalized;
            playerTransform.position += dir * speed * Time.deltaTime;
            yield return null;
        }

        Debug.Log("[CLIENT] Reached target point");

        ServerPullFinished();

        pc.EnableMovement();
        pc.SetLassoState(false);

        isClientPulling = false;
        clientPullCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerPullFinished()
    {
        Debug.Log("[SERVER] Client finished pulling, returning lasso");
        ReturnToPlayer();
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
        if (IsOwner && clientPullCoroutine != null)
        {
            StopCoroutine(clientPullCoroutine);
            clientPullCoroutine = null;
            PlayerController pc = GetPlayerController();
            if (pc != null)
            {
                pc.EnableMovement();
                pc.SetLassoState(false);
            }
            isClientPulling = false;
        }

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
}