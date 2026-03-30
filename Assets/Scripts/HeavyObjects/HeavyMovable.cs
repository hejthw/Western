using UnityEngine;
using FishNet.Object;

public class HeavyMovable : NetworkBehaviour
{
    [Header("Направление движения (в локальных координатах)")]
    public Vector3 moveDirection = Vector3.forward;

    [Header("Коллайдеры-триггеры для определения стороны")]
    public Collider frontTrigger;
    public Collider backTrigger;

    [Header("Скорость движения")]
    public float moveSpeed = 5f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    // Определяем, с какой стороны захват (по точке)
    public bool IsFrontHit(Vector3 hitPoint)
    {
        if (frontTrigger == null || backTrigger == null) return true;
        float distToFront = Vector3.Distance(hitPoint, frontTrigger.ClosestPoint(hitPoint));
        float distToBack = Vector3.Distance(hitPoint, backTrigger.ClosestPoint(hitPoint));
        return distToFront < distToBack;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerMove(Vector3 direction, float speed)
    {
        if (!IsServer) return;
        Vector3 newPos = transform.position + direction * speed * Time.fixedDeltaTime;
        if (rb != null)
            rb.MovePosition(newPos);
        else
            transform.position = newPos;
    }
}