using System.Collections;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Steamworks;
using Unity.Cinemachine;
using Unity.VisualScripting;
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


    [Header("Debug")]
    [Tooltip("Периодические логи владельца во время лазания по верёвке (позиция, скорость, kinematic, NT client-auth).")]
    [SerializeField] private bool debugRopeClimbTelemetry;
    [SerializeField] private int ropeDebugEveryNFrames = 12;
    
    [Header("Anti-Desync Settings")]
    [Tooltip("Интервал проверки позиции на сервере (секунды)")]
    [SerializeField] private float positionValidationInterval = 5f;
    [Tooltip("Максимальное отклонение позиции перед принудительной синхронизацией")]
    [SerializeField] private float maxPositionDeviation = 5f;
    [Tooltip("Включить систему валидации позиции (отключите если есть проблемы с движением)")]
    [SerializeField] private bool enablePositionValidation = false; // По умолчанию отключено
    // вынести отсюда
    private Rigidbody rb;
    private PlayerRotate playerRotate;
    [SerializeField] private float kinematicDelay = 4f;
    public Revolver GetCurrentWeapon() => _currentWeapon;
    private bool _isDied;
    private bool movementDisabled = false;
    
    private Revolver _currentWeapon;
    public float _recoilBuff;
    
    // Антидесинк системы
    private Vector3 _lastValidatedPosition;
    private float _lastPositionValidationTime;

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
    /// Локально на этом экземпляре (владелец): сброс целей NT после выхода из принудительного движения.
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
        if (nt == null || !nt.enabled) return;
        try
        {
            nt.Teleport();
            nt.ForceSend();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerController] ApplyNetworkTransformResyncLocal: {ex.Message}");
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
    /// Телепорт с верёвки с принудительной синхронизацией позиции.
    /// Владелец применяет позицию сам (законный client-auth апдейт).
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


    [ServerRpc]
    private void TakeDamageTest(int damage)
    {
        health.TakeDamage(damage);
    }
    public void SetLassoState(bool state)
    {
        IsLassoPulling = state;

        // Отключаем/включаем систему движения
        if (physics != null)
            physics.enabled = !state;

        if (playerRotate != null)
            playerRotate.enabled = !state;

        // При выходе из лассо-режима принудительно синхронизируем позицию
        if (!state && IsOwner)
        {
            StartCoroutine(DelayedPositionSyncAfterLasso());
        }

        if (debugRopeClimbTelemetry)
        {
            string who = IsServer ? "SERVER (host)" : "CLIENT";
            Debug.Log($"[RopeDbg] [{who}] Lasso state: {state} | Physics enabled: {!state}");
        }
    }
    
    /// <summary>
    /// Отложенная синхронизация после выхода из режима лассо для предотвращения десинхронизации
    /// </summary>
    private IEnumerator DelayedPositionSyncAfterLasso()
    {
        // Ждем один кадр чтобы все системы обновились
        yield return null;
        
        // Сбрасываем физические силы
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Принудительно синхронизируем позицию
        NotifyNetworkTransformHardSync();
        
        // Дополнительно сбрасываем предсказания движения
        if (physics != null)
        {
            physics.ResetOwnerMovementPredictionAfterForcedMove();
            physics.ClearStaleMotionAfterNetworkSnap();
        }
        
        if (debugRopeClimbTelemetry)
        {
            Debug.Log("[RopeDbg] Delayed position sync after lasso completed");
        }
    }

    private void LateUpdate()
    {
        // Система валидации позиции на сервере
        if (IsServer && enablePositionValidation)
        {
            HandleServerPositionValidation();
        }
        
        // Дебаг информация для веревки
        if (!debugRopeClimbTelemetry || !IsOwner || rb == null)
            return;
        if (ActiveRopeClimb == null)
            return;
        if (ropeDebugEveryNFrames > 1 && Time.frameCount % ropeDebugEveryNFrames != 0)
            return;

        NetworkTransform nt = GetComponent<NetworkTransform>();
        bool ntCa = nt != null && NetworkTransformAuthorityUtil.GetClientAuthoritative(nt);
        string role = IsServerInitialized ? "HOST" : "CLIENT";
        Debug.Log(
            $"[RopeDbg tick] {role} pos={rb.position} vel={rb.linearVelocity} mag={rb.linearVelocity.magnitude:F3} " +
            $"kin={rb.isKinematic} physOn={physics != null && physics.enabled} ntClientAuth={ntCa}");
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
        
        // Улучшенный сброс предсказания движения
        if (physics != null)
        {
            physics.ResetOwnerMovementPredictionAfterForcedMove();
            physics.ClearStaleMotionAfterNetworkSnap();
        }
        
        SetLassoState(false);
        
        // Дополнительная принудительная синхронизация для предотвращения скольжения
        if (IsOwner)
        {
            StartCoroutine(DelayedSyncAfterRopeClimb());
        }
    }
    
    /// <summary>
    /// Дополнительная синхронизация после завершения лазания по веревке
    /// </summary>
    private IEnumerator DelayedSyncAfterRopeClimb()
    {
        // Ждем несколько кадров для стабилизации
        yield return null;
        yield return null;
        
        NotifyNetworkTransformHardSync();
        
        if (debugRopeClimbTelemetry)
        {
            Debug.Log("[RopeDbg] Delayed sync after rope climb completed");
        }
    }
    
    /// <summary>
    /// Серверная валидация позиции игрока для предотвращения десинхронизации
    /// </summary>
    [Server]
    private void HandleServerPositionValidation()
    {
        if (Owner == null || !Owner.IsActive)
            return;
            
        // ВАЖНО: Пропускаем валидацию для хоста (он сам себе сервер)
        if (IsHost)
            return;
            
        // Пропускаем валидацию для мертвых игроков или во время принудительного движения
        if (_isDied || IsLassoPulling || ActiveRopeClimb != null)
            return;
            
        float currentTime = Time.time;
        if (currentTime - _lastPositionValidationTime < positionValidationInterval)
            return;
            
        _lastPositionValidationTime = currentTime;
        Vector3 currentPosition = transform.position;
        
        // Первая валидация - просто записываем позицию
        if (_lastValidatedPosition == Vector3.zero)
        {
            _lastValidatedPosition = currentPosition;
            return;
        }
        
        // Проверяем скорость движения
        float distance = Vector3.Distance(_lastValidatedPosition, currentPosition);
        float timeElapsed = positionValidationInterval;
        float speed = distance / timeElapsed;
        
        // Максимальная допустимая скорость (спринт + запас) - сделали еще более мягкой
        float maxAllowedSpeed = physics != null ? physics.GetMaxSprintSpeed() * 3f : 20f; // Увеличили лимит
        
        // Синхронизируем только при экстремальных скоростях
        if (speed > maxAllowedSpeed)
        {
            Debug.LogWarning($"[AntiDesync] Player {Owner.ClientId} moving extremely fast: {speed:F2} > {maxAllowedSpeed:F2}, forcing sync");
            ServerForcePositionCorrection();
        }
        
        _lastValidatedPosition = currentPosition;
    }
    
    /// <summary>
    /// Принудительная коррекция позиции с сервера
    /// </summary>
    [Server]
    private void ServerForcePositionCorrection()
    {
        if (Owner == null || !Owner.IsActive)
            return;
            
        // Отправляем клиенту команду на принудительную синхронизацию
        TargetForcePositionSync(Owner);
        
        // Также уведомляем всех наблюдателей
        ServerBroadcastPostForcedMoveResync();
    }
    
    /// <summary>
    /// Принудительная синхронизация позиции для конкретного клиента
    /// </summary>
    [TargetRpc]
    private void TargetForcePositionSync(NetworkConnection conn)
    {
        if (!IsOwner)
            return;
            
        Debug.Log("[AntiDesync] Received force position sync from server");
        
        // Полный сброс состояния движения
        if (physics != null)
        {
            physics.FullMovementStateReset();
        }
        
        // Принудительная синхронизация NetworkTransform
        NotifyNetworkTransformHardSync();
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
