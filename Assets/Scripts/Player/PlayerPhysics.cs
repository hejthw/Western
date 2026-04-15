using Unity.Cinemachine;
using UnityEngine;
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

    private void Awake() => _player = new Player(data);

    private void OnEnable()
    {
        input.JumpPressedEvent += Jump;
    }

    private void OnDisable()
    {
        input.JumpPressedEvent -= Jump;
    }

    void Update()
    {
        if (!IsOwner)
        {
            ApplyRemoteSmoothing();
            return;
        }
        
        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            data.groundCheckDistance,
            data.whatIsGround);

        bool canSprint = input.SprintHeld;
        _player.ChangeMaxSpeed(canSprint, Time.deltaTime);

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
        Vector3 momentum   = _player.UpdateMomentum(desiredDir, Time.fixedDeltaTime);

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

        Vector3 forward = cinemachineCamera.transform.forward;
        Vector3 right = cinemachineCamera.transform.right;
        forward.y = 0;
        right.y = 0;

        return (forward.normalized * input.MoveInput.y
              + right.normalized   * input.MoveInput.x).normalized;
    }

    // public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    //     => _rb.AddForce(force, mode);
}