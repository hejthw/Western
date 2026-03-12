using UnityEngine;
using System.Collections;

/// <summary>
/// Чистый класс, в котором пока ничего особого нет, но с добавлением прочих фич, думаю можно будет сюда что-то добавить(мб расчет урона или что-то такое)
/// </summary>

public class Player
{
    private readonly PlayerMovementData _data;
    private float _currentSpeed;
    // private float _hp;

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