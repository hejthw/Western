using FishNet.Object;
using UnityEngine;

public class UnMovable : NetworkBehaviour, ILassoInteractable
{
    [Header("Attach Zone")]
    [SerializeField] private Collider attachZone;
    [SerializeField] private float zoneTolerance = 0.15f;

    [Header("Climb Target (legacy / unused for lasso attach)")]
    [SerializeField] private Transform climbAnchor;
    [SerializeField] private Vector3 hitPointOffset = new Vector3(0f, -1.25f, 0f);

    [Header("Rope")]
    [Tooltip("Компонент на том же NetworkObject (или задайте вручную). Активируется лассо в зоне.")]
    [SerializeField] private ClimbRopeNetwork climbRope;

    private void Awake()
    {
        if (climbRope == null)
            climbRope = GetComponent<ClimbRopeNetwork>();
    }

    /// <summary>Сервер: включить верёвку после попадания лассо в зоне.</summary>
    [Server]
    public bool TryActivateRopeFromLassoHit()
    {
        if (climbRope == null)
            return false;
        climbRope.ServerActivateRope();
        return true;
    }

    [Server]
    public bool CanAttach(NetworkObject player)
    {
        if (player == null) return false;
        if (attachZone == null) return false;

        Vector3 playerPos = player.transform.position;
        Vector3 closest = attachZone.ClosestPoint(playerPos);
        float sqrDistance = (closest - playerPos).sqrMagnitude;
        return sqrDistance <= zoneTolerance * zoneTolerance;
    }

    [Server]
    public Vector3 GetClimbTarget(Vector3 hitPoint)
    {
        if (climbAnchor != null)
            return climbAnchor.position;

        return hitPoint + hitPointOffset;
    }

    public void OnLassoAttach(LassoNetwork lasso)
    {
    }

    public void OnLassoPull(LassoNetwork lasso)
    {

    }

    public void OnLassoDetach(LassoNetwork lasso)
    {
    }

    public LassoInteractionType GetInteractionType()
    {
        return LassoInteractionType.PullPlayer;
    }
}