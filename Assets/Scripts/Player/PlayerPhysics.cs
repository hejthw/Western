using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Класс, отвечающий за RigidBody и его логику.
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class PlayerPhysics : MonoBehaviour
{
    [SerializeField] private PlayerMovementData data;
    [SerializeField] private CinemachineCamera  cinemachineCamera;
    [SerializeField] private Transform groundCheck; // точка, где спавниться сфера для просчета IsGrounded

    private Player _player;

    private Rigidbody _rb;
    private PlayerInput _input;

    private bool _isJumping;
    
    public bool IsGrounded { get; private set; }
    public PlayerState CurrentState { get; private set; }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerInput>();

        _player = new Player(data);
    }

    private void OnEnable()
    {
        _input.JumpPressedEvent += Jump;
    }

    private void OnDisable()
    {
        _input.JumpPressedEvent -= Jump;
    }

    void Update()
    {
        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            data.groundCheckDistance,
            data.whatIsGround);

        _player.ChangeMaxSpeed(_input.SprintHeld, Time.deltaTime);

        CurrentState = _player.ResolvePlayerState(
            _input.MoveInput != Vector2.zero,
            IsGrounded,
            _input.SprintHeld,
            _rb.linearVelocity.y);
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        if (_isJumping && !IsGrounded)
            _isJumping = false;
        
        Vector3 dir = GetMoveDirection();

        if (dir == Vector3.zero)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }
        
        Vector3 targetVelocity = dir * _player.CurrentSpeed;
        
        float yVelocity = (IsGrounded && !_isJumping)
            ? 0f                      // если на земле = прижимаем к поверхности (надо для наклонных поверхностей)
            : _rb.linearVelocity.y;   // в воздухе или в прыжке = остается как есть
        
        _rb.linearVelocity = new Vector3(
            targetVelocity.x,
            yVelocity,
            targetVelocity.z);
    }

    private void Jump()
    {
        if (!IsGrounded) return;
        _isJumping = true;
        
        _rb.linearVelocity = new Vector3(
            _rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(Vector3.up * data.jumpForce, ForceMode.Impulse);
    }

    private Vector3 GetMoveDirection()
    {
        if (_input.MoveInput == Vector2.zero) return Vector3.zero;

        Vector3 forward = cinemachineCamera.transform.forward;
        Vector3 right   = cinemachineCamera.transform.right;
        forward.y = 0; right.y = 0;

        return (forward.normalized * _input.MoveInput.y
              + right.normalized   * _input.MoveInput.x).normalized;
    }

    // public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    //     => _rb.AddForce(force, mode);
}