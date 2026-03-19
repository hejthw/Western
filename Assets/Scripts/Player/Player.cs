using UnityEngine;

public class Player
{
    private readonly PlayerMovementData _data;
    private float _currentSpeed;
    private Vector3 _momentum;
    
    public Player(PlayerMovementData data)
    {
        _data = data;
        _currentSpeed = data.walkSpeed;
    }

    public void ChangeMaxSpeed(bool sprintHeld, float deltaTime)
    {
        float targetSpeed = sprintHeld ? _data.sprintSpeed : _data.walkSpeed;
        float acceleration = sprintHeld ? _data.sprintAcceleration : _data.walkAcceleration;
        
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * deltaTime);
    }
    
    public Vector3 UpdateMomentum(Vector3 desiredDirection, float deltaTime)
    {
        if (desiredDirection == Vector3.zero)
        {
            _momentum = Vector3.MoveTowards(_momentum, Vector3.zero, _data.brakingForce * deltaTime);
        }
        else
        {
            Vector3 targetMomentum = desiredDirection * _currentSpeed;
            _momentum = Vector3.MoveTowards(_momentum, targetMomentum, _data.turnSpeed * deltaTime);
        }

        return _momentum;
    }
    
    public PlayerState ResolvePlayerState(
        bool hasInput,
        bool isGrounded,
        bool sprintHeld,
        float verticalVelocity)
    {
        if (!isGrounded)
            return verticalVelocity > 0.1f
                ? PlayerState.STATE_JUMP
                : PlayerState.STATE_FALL;

        if (!hasInput) return PlayerState.STATE_IDLE;
        if (sprintHeld) return PlayerState.STATE_SPRINT;
        return PlayerState.STATE_WALK;
    }
}