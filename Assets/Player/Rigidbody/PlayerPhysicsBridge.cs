using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPhysicsBridge : MonoBehaviour
{
    [SerializeField] private PlayerMovementData data;
    [SerializeField] private CinemachineCamera  cinemachineCamera;
    [SerializeField] private Transform          groundCheck;

    private PlayerMotor         _motor;
    private PlayerJumpLogic     _jump;
    private PlayerStateLogic _stateResolver;

    private Rigidbody         _rb;
    private PlayerInputBridge _input;

    public bool        IsGrounded   { get; private set; }
    public PlayerState CurrentState { get; private set; }

    void Awake()
    {
        _rb    = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerInputBridge>();
        _rb.freezeRotation = true;
        _rb.interpolation  = RigidbodyInterpolation.Interpolate;

        _motor         = new PlayerMotor(data);
        _jump          = new PlayerJumpLogic(data);
        _stateResolver = new PlayerStateLogic();
    }

    void Update()
    {
        IsGrounded = Physics.CheckSphere(
            groundCheck.position,
            data.groundCheckDistance,
            data.whatIsGround);

        _motor.Tick(_input.SprintHeld, Time.deltaTime);
        
        if (_input.JumpPressed)
        {
            _jump.RequestJump();
            _input.ConsumeJump();
        }

        CurrentState = _stateResolver.Resolve(
            _input.MoveInput != Vector2.zero,
            IsGrounded,
            _input.SprintHeld,
            _rb.linearVelocity.y);
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyJump();
    }

    private void ApplyMovement()
    {
        Vector3 dir = GetMoveDirection();
        
        Vector3 targetVelocity = dir * _motor.CurrentSpeed;
        _rb.linearVelocity = new Vector3(
            targetVelocity.x,
            _rb.linearVelocity.y,
            targetVelocity.z);
    }

    private void ApplyJump()
    {
        float? force = _jump.TryConsume(IsGrounded);
        if (force == null) return;

        _rb.linearVelocity = new Vector3(
            _rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(Vector3.up * force.Value, ForceMode.Impulse);
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

    public void AddExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        => _rb.AddForce(force, mode);

    public void SetKinematic(bool state) => _rb.isKinematic = state;
}