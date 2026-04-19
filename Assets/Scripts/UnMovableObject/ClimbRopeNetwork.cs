using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Верёвка: как лассо-притягивание — <see cref="PlayerController.ServerSetForcedMoveNetworkMode"/> (server-auth NT),
/// сервер двигает игрока через Rigidbody.MovePosition; позиция у всех идёт из NetworkTransform.
/// Игнор слоя земли на время лазания. Без SyncVar t и без отключения NT.
/// </summary>
public class ClimbRopeNetwork : NetworkBehaviour
{
    [Header("Visual / interact")]
    [SerializeField] private GameObject ropeVisualRoot;
    [SerializeField] private Collider interactCollider;

    [Header("Climb")]
    [SerializeField] private Transform climbBottom;
    [SerializeField] private Transform climbTop;
    [SerializeField] private float climbSpeed = 4f;
    [SerializeField] private float interactMaxDistance = 3.5f;
    [SerializeField] private float topArrivalDistance = 0.65f;
    [SerializeField] private float finishBackoffDistance = 0.35f;
    [SerializeField] private float finishSettleTime = 0.08f;

    [Header("Jump off")]
    [SerializeField] private float jumpOffForce = 7f;
    [SerializeField] private float jumpOffForwardFactor = 0.55f;

    [Header("Debug")]
    [SerializeField] private bool debugClimbServer;

    private readonly SyncVar<bool> _ropeActive = new SyncVar<bool>();

    private NetworkObject _climber;
    private Rigidbody _climberRb;
    private PlayerController _climberPc;
    private PlayerPhysics _climberPh;
    private Coroutine _climbCoroutine;
    private bool _finishing;
    private float _ropeWorldLength;
    /// <summary>Только на сервере: прогресс 0…1 вдоль верёвки (не реплицируется).</summary>
    private float _climbT;

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

    [Server]
    public void ServerTryBeginClimb(NetworkObject player)
    {
        if (!_ropeActive.Value) return;
        if (player == null) return;
        if (_climber != null) return;
        if (climbBottom == null || climbTop == null)
            return;

        if (interactCollider != null)
        {
            Vector3 closest = interactCollider.ClosestPoint(player.transform.position);
            if ((closest - player.transform.position).sqrMagnitude > interactMaxDistance * interactMaxDistance)
                return;
        }
        else
        {
            Vector3 closest = ClosestPointOnRope(player.transform.position);
            if ((closest - player.transform.position).sqrMagnitude > interactMaxDistance * interactMaxDistance)
                return;
        }

        _climber = player;
        _climberRb = player.GetComponent<Rigidbody>();
        _climberPc = player.GetComponent<PlayerController>();
        _climberPh = player.GetComponent<PlayerPhysics>();
        if (_climberRb == null || _climberPc == null || _climberPh == null)
        {
            ClearClimberRefs();
            return;
        }

        _ropeWorldLength = Vector3.Distance(climbBottom.position, climbTop.position);

        Vector3 startOnRope = ClosestPointOnRope(player.transform.position);
        Vector3 abw = climbTop.position - climbBottom.position;
        float denom = abw.sqrMagnitude;
        float t0 = denom > 1e-6f
            ? Mathf.Clamp01(Vector3.Dot(startOnRope - climbBottom.position, abw) / denom)
            : 0f;

        // Как Lasso.StartPlayerPullServer: сначала режим NT, иначе риск Kick ExploitAttempt.
        _climberPc.ServerSetForcedMoveNetworkMode(true);
        _climberPh.ServerSetGroundCollisionIgnoreForRope(true);

        _climbT = t0;

        ApplyRbPulledStateLocal(_climber, true);
        ApplyClimberWorldPosAtT(_climber, _climbT);
        RpcSetClimberPulledState(_climber.ObjectId, true);
        _climberPc.TargetBeginRopeClimb(_climber.Owner, base.NetworkObject);

        if (debugClimbServer)
        {
            Debug.Log($"[ClimbRope] NT+forced start player={player.ObjectId} t0={t0:F3} len={_ropeWorldLength:F2}");
        }

        if (_climbCoroutine != null)
            StopCoroutine(_climbCoroutine);
        _climbCoroutine = StartCoroutine(ServerClimbProgressCoroutine());
    }

    [Server]
    public void ServerPerformJumpOff(NetworkObject player)
    {
        if (player == null || _climber != player) return;
        if (_climberRb == null) return;

        Vector3 jumpDirection = (Vector3.up + player.transform.forward * jumpOffForwardFactor).normalized;
        EndClimbServer(jump: true, jumpDirection * jumpOffForce);
    }

    private IEnumerator ServerClimbProgressCoroutine()
    {
        while (_climber != null && _ropeActive.Value && !_finishing)
        {
            float curT = _climbT;
            Vector3 atT = Vector3.Lerp(climbBottom.position, climbTop.position, Mathf.Clamp01(curT));
            float distToTop = Vector3.Distance(atT, climbTop.position);

            if (distToTop <= topArrivalDistance || curT >= 1f - 1e-5f)
            {
                float tEnd = curT;
                if (distToTop > 0.001f && distToTop <= topArrivalDistance)
                {
                    Vector3 toTop = climbTop.position - atT;
                    float back = Mathf.Min(finishBackoffDistance, topArrivalDistance);
                    Vector3 adjusted = atT + toTop.normalized * Mathf.Max(0f, distToTop - back);
                    Vector3 ab = climbTop.position - climbBottom.position;
                    float d2 = ab.sqrMagnitude;
                    tEnd = d2 > 1e-6f
                        ? Mathf.Clamp01(Vector3.Dot(adjusted - climbBottom.position, ab) / d2)
                        : 1f;
                }
                else
                    tEnd = 1f;

                _climbT = Mathf.Clamp01(tEnd);
                if (_climber != null)
                    ApplyClimberWorldPosAtT(_climber, _climbT);
                if (!_finishing)
                {
                    _climbCoroutine = null;
                    StartCoroutine(FinishClimbAfterSettle());
                }
                yield break;
            }

            float seg = Mathf.Max(_ropeWorldLength, 0.05f);
            float deltaT = (climbSpeed * Time.fixedDeltaTime) / seg;
            _climbT = Mathf.Min(1f, curT + deltaT);
            if (_climber != null)
                ApplyClimberWorldPosAtT(_climber, _climbT);

            yield return new WaitForFixedUpdate();
        }
    }

    [Server]
    private IEnumerator FinishClimbAfterSettle()
    {
        _finishing = true;
        _climbT = 1f;
        if (_climber != null)
            ApplyClimberWorldPosAtT(_climber, 1f);
        float t = 0f;
        while (t < finishSettleTime)
        {
            if (_climber == null)
                yield break;
            ApplyClimberWorldPosAtT(_climber, _climbT);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndClimbServer(jump: false, Vector3.zero);
    }

    [Server]
    private void EndClimbServer(bool jump, Vector3 impulse)
    {
        if (_climber == null) return;

        NetworkObject climberObj = _climber;
        NetworkConnection ownerConn = climberObj.Owner;
        PlayerPhysics endPh = climberObj.GetComponent<PlayerPhysics>();

        if (endPh != null)
            endPh.ServerSetGroundCollisionIgnoreForRope(false);

        // Прыжок: как Lasso.ServerJumpOffPull — снять kinematic и импульс до выхода из forced NT.
        if (jump && _climberRb != null)
        {
            ApplyRbPulledStateLocal(climberObj, false);
            RpcSetClimberPulledState(climberObj.ObjectId, false);
            _climberRb.linearVelocity = Vector3.zero;
            _climberRb.AddForce(impulse, ForceMode.Impulse);
            RpcApplyJumpOffClimber(climberObj.ObjectId, impulse);
        }

        if (_climberPc != null)
        {
            _climberPc.ServerSetForcedMoveNetworkMode(false);
            _climberPc.ServerBroadcastPostForcedMoveResync();
            _climberPc.TargetEndRopeClimb(ownerConn, fromJump: jump);
        }

        // Финиш наверху без прыжка: как Lasso.ReturnToPlayer — RB после Target.
        if (!jump)
        {
            ApplyRbPulledStateLocal(climberObj, false);
            RpcSetClimberPulledState(climberObj.ObjectId, false);
        }

        if (_climbCoroutine != null)
        {
            StopCoroutine(_climbCoroutine);
            _climbCoroutine = null;
        }

        ClearClimberRefs();
        _finishing = false;
    }

    private void ClearClimberRefs()
    {
        _climber = null;
        _climberRb = null;
        _climberPc = null;
        _climberPh = null;
    }

    private Vector3 ClosestPointOnRope(Vector3 worldPos)
    {
        if (climbBottom == null || climbTop == null)
            return worldPos;

        Vector3 a = climbBottom.position;
        Vector3 b = climbTop.position;
        Vector3 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 0.0001f)
            return a;
        float t = Mathf.Clamp01(Vector3.Dot(worldPos - a, ab) / denom);
        return a + ab * t;
    }

    [ObserversRpc]
    private void RpcApplyJumpOffClimber(int playerObjectId, Vector3 impulse)
    {
        if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(playerObjectId, out NetworkObject playerObj))
            return;

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    private void ApplyClimberWorldPosAtT(NetworkObject nob, float tAlongRope)
    {
        if (nob == null || climbBottom == null || climbTop == null)
            return;

        Vector3 worldPos = Vector3.Lerp(climbBottom.position, climbTop.position, Mathf.Clamp01(tAlongRope));
        Rigidbody rb = nob.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
            rb.MovePosition(worldPos);
        else
            nob.transform.position = worldPos;
    }

    private void ApplyRbPulledStateLocal(NetworkObject playerObj, bool active)
    {
        if (playerObj == null) return;

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (active)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [ObserversRpc]
    private void RpcSetClimberPulledState(int playerObjectId, bool active)
    {
        if (IsServerInitialized) return;
        if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(playerObjectId, out NetworkObject playerObj))
            return;

        ApplyRbPulledStateLocal(playerObj, active);
    }
}
