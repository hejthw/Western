using UnityEngine;
using System.Collections;


public class Player
{
    private readonly PlayerMovementData _data;
    private float _currentSpeed;

    public float CurrentSpeed => _currentSpeed;

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