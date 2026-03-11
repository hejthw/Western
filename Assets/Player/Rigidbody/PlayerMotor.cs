using UnityEngine;

public class PlayerMotor
{
    private readonly PlayerMovementData _data;
    private float _currentSpeed;

    public float CurrentSpeed => _currentSpeed;

    public PlayerMotor(PlayerMovementData data)
    {
        _data = data;
        _currentSpeed = data.walkSpeed;
    }

    public void Tick(bool sprintHeld, float deltaTime)
    {
        float target = sprintHeld ? _data.sprintSpeed : _data.walkSpeed;
        float rate   = sprintHeld ? _data.sprintAcceleration : _data.walkAcceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, rate * deltaTime);
    }

    public float GetDrag(bool isGrounded) =>
        isGrounded ? _data.groundDrag : _data.airDrag;
}