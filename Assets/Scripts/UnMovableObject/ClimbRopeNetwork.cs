using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Верёвка появляется после попадания лассо (см. <see cref="UnMovable.TryActivateRopeFromLassoHit"/>).
/// Телепорт: <see cref="PlayerController.ServerTeleportFromRope"/> — без смены authority NT (иначе кик FishNet).
/// </summary>
public class ClimbRopeNetwork : NetworkBehaviour
{
    [Header("Visual / interact")]
    [SerializeField] private GameObject ropeVisualRoot;
    [SerializeField] private Collider interactCollider;

    [Header("Teleport")]
    [SerializeField] private Transform climbBottom;
    [SerializeField] private Transform climbTop;
    [Tooltip("Если задан — игрок оказывается здесь; иначе используется climbTop.")]
    [SerializeField] private Transform teleportPoint;
    [SerializeField] private float interactMaxDistance = 3.5f;

    [Header("Debug")]
    [SerializeField] private bool debugRopeServer;

    private readonly SyncVar<bool> _ropeActive = new SyncVar<bool>();

    public bool IsRopeActive => _ropeActive.Value;

    private void Start()
    {
        _ropeActive.OnChange += OnRopeActiveChanged;
        ApplyRopeVisual(_ropeActive.Value);
    }

    private void OnDestroy()
    {
        _ropeActive.OnChange -= OnRopeActiveChanged;
    }

    private void OnRopeActiveChanged(bool prev, bool next, bool asServer)
    {
        ApplyRopeVisual(next);
    }

    private void ApplyRopeVisual(bool active)
    {
        if (ropeVisualRoot != null)
            ropeVisualRoot.SetActive(active);
        if (interactCollider != null)
            interactCollider.enabled = active;
    }

    [Server]
    public void ServerActivateRope()
    {
        _ropeActive.Value = true;
    }

    /// <summary>Сервер: взаимодействие с верёвкой — телепорт к верхней точке.</summary>
    [Server]
    public void ServerTryTeleportToTop(NetworkObject player)
    {
        if (!_ropeActive.Value || player == null) return;

        Vector3 destination = ResolveTeleportWorldPosition();
        if (float.IsNaN(destination.x))
            return;

        if (!ValidateInteractDistance(player.transform.position))
            return;

        PlayerController pc = player.GetComponent<PlayerController>();
        PlayerPhysics ph = player.GetComponent<PlayerPhysics>();
        if (pc == null || ph == null)
            return;

        Quaternion rotation = ResolveTeleportRotation();

        StartCoroutine(ServerRopeTeleportWithGroundIgnoreRoutine(ph, pc, destination, rotation));

        if (debugRopeServer)
            Debug.Log($"[ClimbRope] Teleport player={player.ObjectId} to {destination}");
    }

    /// <summary>Игнор пола один кадр до/после снапа — иначе true/false в том же кадре не даёт эффекта (RunLocally).</summary>
    private IEnumerator ServerRopeTeleportWithGroundIgnoreRoutine(
        PlayerPhysics ph,
        PlayerController pc,
        Vector3 destination,
        Quaternion rotation)
    {
        ph.ServerSetGroundCollisionIgnoreForRope(true);
        yield return null;
        pc.ServerTeleportFromRope(destination, rotation);
        yield return null;
        ph.ServerSetGroundCollisionIgnoreForRope(false);
    }

    /// <summary>Оставлено для совместимости с <see cref="PlayerController.ServerRequestRopeJumpOff"/>.</summary>
    [Server]
    public void ServerPerformJumpOff(NetworkObject player) { }

    private Vector3 ResolveTeleportWorldPosition()
    {
        if (teleportPoint != null)
            return teleportPoint.position;
        if (climbTop != null)
            return climbTop.position;
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    private Quaternion ResolveTeleportRotation()
    {
        if (teleportPoint != null)
            return teleportPoint.rotation;
        if (climbTop != null)
            return climbTop.rotation;
        return Quaternion.identity;
    }

    private bool ValidateInteractDistance(Vector3 playerPos)
    {
        if (interactCollider != null)
        {
            Vector3 closest = interactCollider.ClosestPoint(playerPos);
            if ((closest - playerPos).sqrMagnitude > interactMaxDistance * interactMaxDistance)
                return false;
            return true;
        }

        if (climbBottom != null && climbTop != null)
        {
            Vector3 closest = ClosestPointOnSegment(climbBottom.position, climbTop.position, playerPos);
            if ((closest - playerPos).sqrMagnitude > interactMaxDistance * interactMaxDistance)
                return false;
            return true;
        }

        return climbTop != null;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-6f)
            return a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
        return a + ab * t;
    }
}
