using Unity.Cinemachine;
using UnityEngine;
using FishNet.Component.Transforming;
using FishNet.Object;
using System.Collections;

/// <summary>
/// Класс, отвечающий за RigidBody и его логику.
/// </summary>

public class PlayerPhysics : NetworkBehaviour
{
    [SerializeField] private PlayerMovementData data;
    [SerializeField] private CinemachineCamera  cinemachineCamera;
    [SerializeField] private Transform groundCheck; // точка, где спавниться сфера для просчета IsGrounded
    
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerInput input;
    [SerializeField] private Collider col;
    
    // Система антидесинка
    private float _lastCollisionSyncTime;

    private float _currentSpeed;
    private float _speedBuff;
    private float _walkDebuff;
    private Vector3 _momentum;
    
    private Player _player;
    private bool _isJumping;
    private bool _isFalling;
    private float _fallStartY;
    public PlayerHealth _playerHealth;
    
    [Header("Fall Damage")]
    [SerializeField] private float fallDamageCoef = 1f;
    [SerializeField] private float characterHeight = 2f;
    
    [Header("Anti-Desync Settings")]
    [Tooltip("Включить проверку на аномальные скорости")]
    [SerializeField] private bool enableAnomalyDetection = false; // По умолчанию отключено
    [Tooltip("Включить синхронизацию при столкновениях")]
    [SerializeField] private bool enableCollisionSync = false; // По умолчанию отключено 
    
    public bool IsGrounded { get; private set; }
    public PlayerState CurrentState { get; private set; }
    
    /// <summary>
    /// Получить максимальную скорость спринта (для системы валидации)
    /// </summary>
    public float GetMaxSprintSpeed()
    {
        return data != null ? data.sprintSpeed : 7f; // Значение по умолчанию
    }
    
    private void Awake()
    {
        _player = new Player();
    }

    private void OnEnable()
    {
        input.JumpPressedEvent += Jump;
        PlayerEffectsEvents.OnSpeedBuff += ChangeSpeedBuff;
        PlayerEffectsEvents.OnWalkDebuff += ChangeWalkDebuff;
    }
    
    private void OnDisable()
    {
        input.JumpPressedEvent -= Jump;
        PlayerEffectsEvents.OnSpeedBuff -= ChangeSpeedBuff;
        PlayerEffectsEvents.OnWalkDebuff -= ChangeWalkDebuff;
    }

    void Update()
    {
        if (!IsOwner)
        {
            // Удалённые копии полностью синхронизируются через NetworkTransform.
            return;
        }
        
        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            data.groundCheckDistance,
            data.whatIsGround);

        bool canSprint = input.SprintHeld;
        ChangeMaxSpeed(canSprint, Time.deltaTime);

        CurrentState = _player.ResolvePlayerState(
            input.MoveInput != Vector2.zero,
            IsGrounded,
            input.SprintHeld,
            rb.linearVelocity.y);
        
        // Начало падения
        if (CurrentState == PlayerState.STATE_FALL && !_isFalling)
        {
            _isFalling  = true;
            _fallStartY = transform.position.y;
        }

        // Приземление
        if (_isFalling && IsGrounded)
        {
            _isFalling = false;
            float height = _fallStartY - transform.position.y;

            if (height > characterHeight)
            {
                float excess = height - characterHeight;
                int damage = Mathf.RoundToInt(fallDamageCoef * excess * excess);
                if (damage > 0)
                    RequestFallDamageServerRpc(damage);
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        ApplyMovement();
        
        // Периодическая проверка состояния для владельца (только если включено)
        if (enableAnomalyDetection)
        {
            CheckForAnomalousState();
        }
    }
    
    /// <summary>
    /// Проверка на аномальные состояния движения
    /// </summary>
    private void CheckForAnomalousState()
    {
        if (rb == null || !IsOwner) return;
        
        // Пропускаем для хостов
        if (IsHost) return;
        
        // Делаем проверку реже - только каждые 2 секунды
        if (Time.fixedTime % 2f > Time.fixedDeltaTime) return;
        
        // Проверяем на застревание в геометрии (более строгие условия)
        if (rb.linearVelocity.magnitude < 0.05f && input.MoveInput.magnitude > 0.8f && _currentSpeed > 2f)
        {
            // Игрок пытается двигаться, но не движется - возможно застрял
            Vector3 testDirection = GetMoveDirection();
            if (testDirection != Vector3.zero && Physics.Raycast(transform.position, testDirection, 0.3f, data.whatIsGround))
            {
                Debug.LogWarning("[PlayerPhysics] Player appears stuck in geometry, requesting position sync");
                RequestPositionValidation();
            }
        }
    }
    
    /// <summary>
    /// Запрос валидации позиции с сервера
    /// </summary>
    private void RequestPositionValidation()
    {
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.NotifyNetworkTransformHardSync();
        }
    }
    
    /// <summary>
    /// После Teleport/жёсткого сброса NT: гасим RB velocity, чтобы не тянуть инерцию после снапа.
    /// </summary>
    public void ClearStaleMotionAfterNetworkSnap()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>Сброс внутреннего импульса движения у владельца после выхода с верёвки/лассо или физических взаимодействий.</summary>
    public void ResetOwnerMovementPredictionAfterForcedMove()
    {
        if (!IsOwner) return;
        
        _momentum = Vector3.zero;
        _currentSpeed = 0f;
        _isJumping = false;
        _isFalling = false;
        
        // Дополнительно сбрасываем скорости в Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Сохраняем Y для гравитации
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log("[PlayerPhysics] Movement prediction and momentum reset");
    }
    
    /// <summary>
    /// Полный сброс всех состояний движения - используется после серьезных физических взаимодействий
    /// </summary>
    public void FullMovementStateReset()
    {
        if (!IsOwner) return;
        
        _momentum = Vector3.zero;
        _currentSpeed = 0f;
        _isJumping = false;
        _isFalling = false;
        _fallStartY = 0f;
        
        // Сбрасываем все скорости
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Принудительное обновление состояния игрока
        if (input != null)
        {
            CurrentState = _player.ResolvePlayerState(false, IsGrounded, false, 0f);
        }
        
        Debug.Log("[PlayerPhysics] Full movement state reset");
    }

    /// <summary>Игнор столкновений с маской whatIsGround (скольжение по полу во время лазания).</summary>
    [Server]
    public void ServerSetGroundCollisionIgnoreForRope(bool ignore)
    {
        ObserversSetGroundCollisionIgnoreForRope(ignore);
    }

    [ObserversRpc(RunLocally = true)]
    private void ObserversSetGroundCollisionIgnoreForRope(bool ignore)
    {
        ApplyGroundLayerCollisionIgnore(ignore);
    }

    private void ApplyGroundLayerCollisionIgnore(bool ignore)
    {
        if (data == null) return;
        int playerLayer = gameObject.layer;
        int mask = data.whatIsGround.value;
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
                Physics.IgnoreLayerCollision(playerLayer, i, ignore);
        }
    }

    private void ApplyMovement()
    {
        if (_isJumping && !IsGrounded)
            _isJumping = false;

        Vector3 desiredDir = GetMoveDirection();
        Vector3 momentum   = UpdateMomentum(desiredDir, Time.fixedDeltaTime);

        float yVelocity = (IsGrounded && !_isJumping)
            ? 0f
            : rb.linearVelocity.y;

        Vector3 newVelocity = new Vector3(momentum.x, yVelocity, momentum.z);
        
        // Проверка на аномальные скорости (защита от десинхронизации)
        if (enableAnomalyDetection)
        {
            // Делаем проверку менее агрессивной и только для экстремальных случаев
            float maxReasonableSpeed = data.sprintSpeed * 4f; // Увеличили лимит в 2 раза
            Vector3 horizontalVel = new Vector3(newVelocity.x, 0, newVelocity.z);
            
            if (horizontalVel.magnitude > maxReasonableSpeed)
            {
                Debug.LogWarning($"[PlayerPhysics] Extreme velocity detected: {horizontalVel.magnitude}, clamping to {maxReasonableSpeed}");
                horizontalVel = horizontalVel.normalized * maxReasonableSpeed;
                newVelocity = new Vector3(horizontalVel.x, yVelocity, horizontalVel.z);
            }
        }

        rb.linearVelocity = newVelocity;
    }

    private void Jump()
    {
        if (!IsGrounded) return;
        SoundBus.Play(SoundID.PlayerJump);
        _isJumping = true;
        
        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * data.jumpForce, ForceMode.Impulse);
    }

    private Vector3 GetMoveDirection()
    {
        if (input.MoveInput == Vector2.zero) return Vector3.zero;

        Vector3 forward = GetForward();
        Vector3 right = GetRight();
        
        return (forward.normalized * input.MoveInput.y
              + right.normalized * input.MoveInput.x).normalized;
    }

    private Vector3 GetForward()
    {
        Vector3 a = cinemachineCamera.transform.forward;
        float x = Random.Range(-_walkDebuff, _walkDebuff);
        float z = Random.Range(-_walkDebuff, _walkDebuff);
        return new Vector3(a.x + x, 0, a.z + z);
    }
    
    private Vector3 GetRight()
    {
        Vector3 a = cinemachineCamera.transform.right;
        float x = Random.Range(-_walkDebuff, _walkDebuff);
        float z = Random.Range(-_walkDebuff, _walkDebuff);
        return new Vector3(a.x + x, 0, a.z + z);
    }
    
    private void ChangeSpeedBuff(float buff) => _speedBuff = buff;
    private void ChangeWalkDebuff(float buff) => _walkDebuff = buff;
    
    private void ChangeMaxSpeed(bool sprintHeld, float deltaTime)
    {
        var sprintSpeed = data.sprintSpeed;
        var walkSpeed = data.walkSpeed;
        
        if (_speedBuff != 0)
        {
            sprintSpeed += (sprintSpeed * _speedBuff);
            walkSpeed += (walkSpeed * _speedBuff);
        }
        
        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        float acceleration = sprintHeld ? data.sprintAcceleration : data.walkAcceleration;
        
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * deltaTime);
    }
    
    private Vector3 UpdateMomentum(Vector3 desiredDirection, float deltaTime)
    {
        if (desiredDirection == Vector3.zero)
        {
            _momentum = Vector3.MoveTowards(_momentum, Vector3.zero, data.brakingForce * deltaTime);
        }
        else
        {
            Vector3 targetMomentum = desiredDirection * _currentSpeed;
            _momentum = Vector3.MoveTowards(_momentum, targetMomentum, data.turnSpeed * deltaTime);
        }

        return _momentum;
    }
    
    [ServerRpc]
    private void RequestFallDamageServerRpc(int damage)
    {
        _playerHealth.TakeDamage(damage);
    }

    /// <summary>
    /// Обработчик столкновений для предотвращения десинхронизации
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner || !enableCollisionSync) return;
        
        // Пропускаем синхронизацию для хостов (они сами себе авторитет)
        if (IsHost) return;
        
        // Предотвращаем слишком частые синхронизации (увеличили интервал)
        if (Time.time - _lastCollisionSyncTime < 3f) return;
        
        // Проверяем, было ли столкновение достаточно сильным И НЕ с землей
        float impactForce = collision.relativeVelocity.magnitude;
        bool isGroundCollision = (data.whatIsGround.value & (1 << collision.gameObject.layer)) != 0;
        
        // Синхронизируем только при сильных ударах НЕ о землю
        if (impactForce > 5f && !isGroundCollision) // Увеличили порог и исключили землю
        {
            _lastCollisionSyncTime = Time.time;
            
            // Небольшая задержка для стабилизации физики
            StartCoroutine(DelayedSyncAfterCollision());
        }
    }
    
    /// <summary>
    /// Отложенная синхронизация после столкновения
    /// </summary>
    private IEnumerator DelayedSyncAfterCollision()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        // Сбрасываем предсказания движения
        ResetOwnerMovementPredictionAfterForcedMove();
        
        // Запрашиваем синхронизацию позиции
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.NotifyNetworkTransformHardSync();
        }
    }

    // public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    //     => _rb.AddForce(force, mode);
}