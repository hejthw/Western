using Unity.Cinemachine;
using UnityEngine;
using FishNet.Component.Transforming;
using FishNet.Object;

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

    [Tooltip("Если задан (обычно на том же объекте), удалённые копии двигаются только через NetworkTransform — без второго слоя lerp из BroadcastStateObserversRpc.")]
    [SerializeField] private NetworkTransform networkTransform;
   
    private float _currentSpeed;
    private float _speedBuff;
    private float _walkDebuff;
    private Vector3 _momentum;
    
    private Player _player;
    private bool _isJumping;
    private float _sendStateTimer;
    private Vector3 _remoteTargetPos;
    private Quaternion _remoteTargetRot;
    private Vector3 _remoteVelocity;
    private bool _hasRemoteState;
    
    public bool IsGrounded { get; private set; }
    public PlayerState CurrentState { get; private set; }
    
    [Header("Net Sync")]
    [SerializeField] private float stateSendInterval = 0.05f;
    [SerializeField] private float remotePositionLerp = 18f;
    [SerializeField] private float remoteRotationLerp = 20f;
    [SerializeField] private float snapDistance = 3f;

    private void Awake()
    {
        _player = new Player();
        if (networkTransform == null)
            networkTransform = GetComponent<NetworkTransform>();
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
            // Два независимых предиктора позиции (NT + этот lerp) дают дёрганье и «скольжение» при остановке.
            if (networkTransform != null && networkTransform.enabled)
                return;

            ApplyRemoteSmoothing();
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
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        ApplyMovement();
        
        if (IsServerInitialized)
            return;
        
        _sendStateTimer -= Time.fixedDeltaTime;
        if (_sendStateTimer <= 0f)
        {
            _sendStateTimer = stateSendInterval;
            SendStateServerRpc(rb.position, transform.rotation, rb.linearVelocity);
        }
    }
    
    /// <summary>
    /// После Teleport/жёсткого сброса NT: гасим RB velocity и устаревшие цели BroadcastState на наблюдателях
    /// (во время верёвки владелец не шлёт SendState — остаётся старая скорость → у хоста «скольжение» удалённого игрока).
    /// </summary>
    public void ClearStaleMotionAfterNetworkSnap()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (!IsOwner)
        {
            _hasRemoteState = false;
            _remoteVelocity = Vector3.zero;
            _remoteTargetPos = transform.position;
            _remoteTargetRot = transform.rotation;
        }
    }

    /// <summary>Сброс внутреннего импульса движения у владельца после выхода с верёвки/лассо.</summary>
    public void ResetOwnerMovementPredictionAfterForcedMove()
    {
        if (!IsOwner) return;
        _momentum = Vector3.zero;
        _currentSpeed = 0f;
        _isJumping = false;
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

    private void ApplyRemoteSmoothing()
    {
        if (!_hasRemoteState) return;
        
        float distance = Vector3.Distance(transform.position, _remoteTargetPos);
        if (distance > snapDistance)
        {
            rb.position = _remoteTargetPos;
            transform.rotation = _remoteTargetRot;
            rb.linearVelocity = _remoteVelocity;
            return;
        }
        
        Vector3 nextPos = Vector3.Lerp(
            transform.position,
            _remoteTargetPos,
            remotePositionLerp * Time.deltaTime
        );
        
        Quaternion nextRot = Quaternion.Slerp(
            transform.rotation,
            _remoteTargetRot,
            remoteRotationLerp * Time.deltaTime
        );
        
        rb.position = nextPos;
        transform.rotation = nextRot;
        rb.linearVelocity = _remoteVelocity;
    }
    
    [ServerRpc]
    private void SendStateServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        BroadcastStateObserversRpc(position, rotation, velocity);
    }
    
    [ObserversRpc(ExcludeOwner = true)]
    private void BroadcastStateObserversRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        _remoteTargetPos = position;
        _remoteTargetRot = rotation;
        _remoteVelocity = velocity;
        _hasRemoteState = true;
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

        rb.linearVelocity = new Vector3(momentum.x, yVelocity, momentum.z);
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

    // public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    //     => _rb.AddForce(force, mode);
}