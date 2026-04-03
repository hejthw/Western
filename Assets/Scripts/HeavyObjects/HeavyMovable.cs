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

    private bool isMoving;
    private Vector3 targetPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (IsServer && startPosition != null)
        {
            transform.position = startPosition.position;
            targetPosition = startPosition.position;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || !isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            isMoving = false;
            isAtStart = !isAtStart;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPull()
    {
        currentPullCount.Value++;
        if (currentPullCount.Value >= requiredPulls)
        {
            currentPullCount.Value = 0;
            ServerStartMove();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerStartMove()
    {
        if (startPosition == null || endPosition == null) return;

        targetPosition = isAtStart ? endPosition.position : startPosition.position;
        isMoving = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetPullCount()
    {
        currentPullCount.Value = 0;
    }
    public bool IsActiveZone(Vector3 hitPoint)
    {
        if (frontTrigger == null || backTrigger == null) return true;

        float frontDist = Vector3.Distance(frontTrigger.ClosestPoint(hitPoint), hitPoint);
        float backDist = Vector3.Distance(backTrigger.ClosestPoint(hitPoint), hitPoint);

        float threshold = 0.2f;

        bool front = frontDist < threshold;
        bool back = backDist < threshold;

        return isAtStart ? front : back;
    }
}