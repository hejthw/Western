using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class HeavyMovable : NetworkBehaviour
{
    [Header("Позиции")]
    public Transform startPosition;
    public Transform endPosition;

    [Header("Настройки")]
    public int requiredPulls = 3;
    public float moveSpeed = 5f;

    public Collider frontTrigger;
    public Collider backTrigger;

    private Rigidbody rb;
    private bool isAtStart = true;

    private readonly SyncVar<int> currentPullCount = new SyncVar<int>();
    private readonly SyncVar<Vector3> targetPosition = new SyncVar<Vector3>();

    private bool isMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (IsServer && startPosition != null)
            transform.position = startPosition.position;

        targetPosition.OnChange += OnTargetChanged;
    }

    private void OnTargetChanged(Vector3 oldValue, Vector3 newValue, bool asServer)
    {
        if (newValue != Vector3.zero)
            isMoving = true;
    }

    private void FixedUpdate()
    {
        if (!IsServer || !isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition.Value,
            moveSpeed * Time.fixedDeltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition.Value) < 0.01f)
        {
            isMoving = false;
            isAtStart = !isAtStart;
        }
    }

    public void RegisterPull()
    {
        if (!IsServer) return;

        currentPullCount.Value++;

        if (currentPullCount.Value >= requiredPulls)
        {
            currentPullCount.Value = 0;
            ServerStartMove();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerStartMove()
    {
        if (startPosition == null || endPosition == null) return;

        Vector3 newTarget = isAtStart ? endPosition.position : startPosition.position;
        targetPosition.Value = newTarget;
        isMoving = true;
    }

    public void ResetPullCount()
    {
        if (!IsServer) return;
        currentPullCount.Value = 0;
    }

    
    public bool IsActiveZone(Vector3 hitPoint)
    {
        if (frontTrigger == null || backTrigger == null) return true;

        bool front = frontTrigger.bounds.Contains(hitPoint);
        bool back = backTrigger.bounds.Contains(hitPoint);

        return isAtStart ? front : back;
    }
}