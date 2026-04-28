using System.Collections;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public CinemachineCamera cinemachineCamera;

    [SerializeField] private PlayerPhysics physics;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerInput input;
    private Coroutine stunRoutine;
    [SerializeField] public Transform weaponHoldPoint;
    public bool IsLassoPulling { get; set; }

    /// <summary>Активная верёвка для прыжка Space (не только владелец лассо).</summary>
    public ClimbRopeNetwork ActiveRopeClimb { get; private set; }

    /// <summary>Совпадает с последним применённым <see cref="ServerSetForcedMoveNetworkMode"/> (NT server vs client auth).</summary>
    private bool _forcedMoveNetworkModeActive;

    [Header("Debug")]
    [Tooltip("Периодические логи владельца во время лазания по верёвке (позиция, скорость, kinematic, NT client-auth).")]
    [SerializeField] private bool debugRopeClimbTelemetry;
    [SerializeField] private int ropeDebugEveryNFrames = 12;
    // вынести отсюда
    private Rigidbody rb;
    private PlayerRotate playerRotate;
    [SerializeField] private float kinematicDelay = 4f;
    public Revolver GetCurrentWeapon() => _currentWeapon;
    private bool _isDied;
    private bool movementDisabled = false;
    
    private Revolver _currentWeapon;
    public float _recoilBuff;

    [SerializeField] private TutorialUISpawner uiSpawner;
    
    private void ChangeRecoilBuff(float buff)
    {
        _recoilBuff = buff;
    }
    
    public bool IsArmed {get ; private set;}
    
    public void EquipWeapon(Revolver weapon)
    {
        _currentWeapon = weapon;
        IsArmed = true;
    }

    public void UnequipWeapon()
    {
        _currentWeapon = null;
        uiSpawner?.OnRevolverDroppedUp();
        IsArmed = false;
    }

    /// <summary>
    /// Стрельба с револьвера: ServerRpc на игроке (клиент всегда владеет префабом игрока), валидация на сервере.
    /// </summary>
    public void RequestRevolverShoot(NetworkObject revolverNetObj, Vector3 origin, Vector3 direction)
    {
        ServerRevolverShoot(revolverNetObj, origin, direction);
    }

    [ServerRpc]
    private void ServerRevolverShoot(NetworkObject revolverNetObj, Vector3 origin, Vector3 direction)
    {
        if (revolverNetObj == null) return;
        Revolver rev = revolverNetObj.GetComponent<Revolver>();
        if (rev == null) return;
        if (_currentWeapon != rev) return;

        rev.ServerApplyShot(origin, direction);
    }

    void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        health = GetComponent<PlayerHealth>();
        input = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        playerRotate = GetComponent<PlayerRotate>();
    }

    private void OnEnable()
    {
        input.OnTestEvent += Test;
        PlayerEffectsEvents.OnRecoilBuff += ChangeRecoilBuff;
        PlayerHealthEvents.OnKnockoutEvent += DisableMovement;
    }
    
    private void OnDisable()
    {
        input.OnTestEvent -= Test;
        PlayerEffectsEvents.OnRecoilBuff -= ChangeRecoilBuff;
        PlayerHealthEvents.OnKnockoutEvent -= DisableMovement;
    }
    
    // вынести отсюда
    private void DisableMovement(bool isDead)
    {
        physics.enabled = !isDead;
        rb.freezeRotation = !isDead;
        playerRotate.enabled = !isDead;

        if (isDead)
            StartCoroutine(EnableKinematicDelayed());
        else
            rb.isKinematic = false;
    }

    // вынести отсюда
    private IEnumerator EnableKinematicDelayed()
    {
        yield return new WaitForSeconds(kinematicDelay);
        rb.isKinematic = true;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        cinemachineCamera.gameObject.SetActive(IsOwner);
        ConfigureRigidbodyForNetworkTransform();

        if (!IsOwner) DisableLocalComponents();
        PlayerRegistry.Register(this);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        PlayerRegistry.Unregister(this);
    }

    private void DisableLocalComponents()
    {
        GetComponent<PlayerInput>().enabled = false;
        GetComponent<PlayerRotate>().enabled = false;
        enabled = false;
    }

    private void ConfigureRigidbodyForNetworkTransform()
    {
        if (rb == null) return;

        rb.isKinematic = !IsOwner;
        if (!IsOwner)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void Test()
    {
        if (!IsOwner) return;
        TakeDamageTest(10);
    }
    
    public void DisableMovement()
    {
        if (physics != null)
            physics.enabled = false;
    }

    public void EnableMovement()
    {
        if (physics != null)
            physics.enabled = true;
    }

    /// <summary>
    /// Локально на этом экземпляре (владелец): сброс целей синхронизации после выхода из принудительного движения.
    /// Для чужих копий игрока вызывайте <see cref="ServerBroadcastPostForcedMoveResync"/> с сервера.
    /// </summary>
    public void NotifyNetworkTransformHardSync()
    {
        ApplyNetworkTransformResyncLocal();
    }

    private void ApplyNetworkTransformResyncLocal()
    {
        if (!IsSpawned)
            return;

        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt != null && nt.enabled)
        {
            try
            {
                nt.Teleport();
                nt.ForceSend();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerController] ApplyNetworkTransformResyncLocal (NetworkTransform): {ex.Message}");
            }
        }

        if (physics != null)
            physics.ClearStaleMotionAfterNetworkSnap();
    }

    /// <summary>
    /// С сервера: сбрасывает интерполяцию/очередь целей NT у всех наблюдателей этого игрока (и на сервере RunLocally).
    /// Устраняет «скольжение» чужого персонажа после верёвки/лассо.
    /// </summary>
    [Server]
    public void ServerBroadcastPostForcedMoveResync()
    {
        ObserversPostForcedMoveResync();
    }

    [ObserversRpc(RunLocally = true)]
    private void ObserversPostForcedMoveResync()
    {
        ApplyNetworkTransformResyncLocal();
    }

    /// <summary>
    /// Телепорт с верёвки без <see cref="ServerSetForcedMoveNetworkMode"/> — иначе гонка NT и кик
    /// «update without client authority». Владелец применяет позицию сам (законный client-auth апдейт).
    /// </summary>
    [Server]
    public void ServerTeleportFromRope(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!IsSpawned || Owner == null || !Owner.IsActive)
            return;

        ServerApplyRopeTeleportPhysics(worldPosition, worldRotation);
        TargetRopeTeleport(Owner, worldPosition, worldRotation);
    }

    private void ServerApplyRopeTeleportPhysics(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(worldPosition, worldRotation);
        if (rb != null)
        {
            rb.MovePosition(worldPosition);
            rb.MoveRotation(worldRotation);
        }

        Physics.SyncTransforms();
    }

    [TargetRpc]
    private void TargetRopeTeleport(NetworkConnection conn, Vector3 worldPosition, Quaternion worldRotation)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(worldPosition, worldRotation);
        if (rb != null)
        {
            rb.MovePosition(worldPosition);
            rb.MoveRotation(worldRotation);
        }

        Physics.SyncTransforms();

        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt != null)
        {
            nt.Teleport();
            nt.ForceSend();
        }

        if (physics != null)
            physics.ClearStaleMotionAfterNetworkSnap();
    }

    /// <summary>
    /// Принудительное движение с сервера (верёвка, лассо): временно переводим FishNet NetworkTransform
    /// в server-authoritative. Порядок критичен (иначе Kick ExploitAttempt):
    /// вход в режим — сначала владелец перестаёт слать ServerUpdateTransform, потом сервер и наблюдатели;
    /// выход — сначала сервер и наблюдатели принимают client-auth, потом владелец снова шлёт апдейты.
    /// </summary>
    [Server]
    public void ServerSetForcedMoveNetworkMode(bool forcedMoveActive)
    {
        if (!IsSpawned)
            return;
        if (Owner == null || !Owner.IsActive)
            return;

        if (forcedMoveActive)
        {
            TargetApplyForcedMoveNetworkMode(Owner, true);
        }
        else
        {
            ApplyForcedMoveNetworkTransformMode(false);
            ObserversApplyForcedMoveToNonOwners(false);
            TargetApplyForcedMoveNetworkMode(Owner, false);
        }
    }

    [TargetRpc]
    private void TargetApplyForcedMoveNetworkMode(NetworkConnection conn, bool forcedMoveActive)
    {
        ApplyForcedMoveNetworkTransformMode(forcedMoveActive);
        if (forcedMoveActive)
            ServerNotifyForcedMoveNetworkApplied(forcedMoveActive);
    }

    [ServerRpc(RequireOwnership = true)]
    private void ServerNotifyForcedMoveNetworkApplied(bool forcedMoveActive)
    {
        ApplyForcedMoveNetworkTransformMode(forcedMoveActive);
        ObserversApplyForcedMoveToNonOwners(forcedMoveActive);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void ObserversApplyForcedMoveToNonOwners(bool forcedMoveActive)
    {
        ApplyForcedMoveNetworkTransformMode(forcedMoveActive);
    }

    private void ApplyForcedMoveNetworkTransformMode(bool forcedMoveActive)
    {
        if (!IsSpawned)
            return;
        if (_forcedMoveNetworkModeActive == forcedMoveActive)
            return;

        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt == null || !nt.isActiveAndEnabled)
            return;

        bool clientAuthoritative = !forcedMoveActive;
        if (!NetworkTransformAuthorityUtil.SetClientAuthoritative(nt, clientAuthoritative))
            return;

        _forcedMoveNetworkModeActive = forcedMoveActive;

        if (debugRopeClimbTelemetry)
        {
            bool ca = NetworkTransformAuthorityUtil.GetClientAuthoritative(nt);
            string role = IsServerInitialized ? (IsClientInitialized ? "HOST" : "SERVER") : "CLIENT";
            Debug.Log($"[RopeDbg NT] {role} ApplyForcedMove forced={forcedMoveActive} ntClientAuth={ca} pos={transform.position}");
        }
    }

    [ServerRpc]
    private void TakeDamageTest(int damage)
    {
        health.TakeDamage(damage);
    }
    public void SetLassoState(bool state)
    {
        IsLassoPulling = state;

   
        if (physics != null)
            physics.enabled = !state;

        if (playerRotate != null)
            playerRotate.enabled = !state;

      

        if (debugRopeClimbTelemetry)
        {
            string who = IsServer ? "SERVER (host)" : "CLIENT";
            Debug.Log($"[RopeDbg] [{who}] Lasso state: {state} | Physics enabled: {!state}");
        }
    }

    private void LateUpdate()
    {
        if (!debugRopeClimbTelemetry || !IsOwner || rb == null)
            return;
        if (ActiveRopeClimb == null)
            return;
        if (ropeDebugEveryNFrames > 1 && Time.frameCount % ropeDebugEveryNFrames != 0)
            return;

        string syncType = "None";
        bool syncAuth = false;

        NetworkTransform nt = GetComponent<NetworkTransform>();
        if (nt != null)
        {
            syncType = "NetworkTransform";
            syncAuth = NetworkTransformAuthorityUtil.GetClientAuthoritative(nt);
        }
        
        string role = IsServerInitialized ? "HOST" : "CLIENT";
        Debug.Log(
            $"[RopeDbg tick] {role} pos={rb.position} vel={rb.linearVelocity} mag={rb.linearVelocity.magnitude:F3} " +
            $"kin={rb.isKinematic} physOn={physics != null && physics.enabled} sync={syncType} syncAuth={syncAuth} forcedMode={_forcedMoveNetworkModeActive}");
    }

    [TargetRpc]
    public void TargetBeginRopeClimb(NetworkConnection conn, NetworkObject ropeNetObj)
    {
        ActiveRopeClimb = ropeNetObj != null ? ropeNetObj.GetComponent<ClimbRopeNetwork>() : null;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        DisableMovement();
        SetLassoState(true);

        if (debugRopeClimbTelemetry)
        {
            string role = IsServerInitialized ? "HOST" : "CLIENT";
            Debug.Log($"[RopeDbg] TargetBeginRopeClimb {role} rope={(ropeNetObj != null)}");
        }
    }

    [TargetRpc]
    public void TargetEndRopeClimb(NetworkConnection conn, bool fromJump)
    {
        ActiveRopeClimb = null;
        if (rb != null)
        {
            rb.isKinematic = false;
            if (!fromJump)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        EnableMovement();
        if (physics != null)
            physics.ResetOwnerMovementPredictionAfterForcedMove();
        SetLassoState(false);
    }

    public void RequestRopeJumpOff(ClimbRopeNetwork rope)
    {
        if (rope == null) return;
        ServerRequestRopeJumpOff(rope.NetworkObject);
    }

    [ServerRpc]
    private void ServerRequestRopeJumpOff(NetworkObject ropeNetObj)
    {
        if (ropeNetObj == null) return;
        ClimbRopeNetwork rope = ropeNetObj.GetComponent<ClimbRopeNetwork>();
        if (rope == null) return;
        rope.ServerPerformJumpOff(NetworkObject);
    }
    

    public void Stun(float duration)
    {
        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        // Отключаем управление (как при Knockout)
        DisableMovement();
        SetLassoState(true); // отключает физику и поворот, если нужно

        yield return new WaitForSeconds(duration);

        // Восстанавливаем управление
        EnableMovement();
        SetLassoState(false);

        stunRoutine = null;
    }
    [Server]
    public void ForceDropWeapon()
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.DropToGround();
        }
        else
        {
            PickupController pickup = GetComponent<PickupController>();
            if (pickup != null && pickup.GetHeldObject() != null)
            {
                pickup.Throw();
            }
        }
    }
}
