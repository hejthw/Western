using UnityEngine;

/// <summary>
/// SO, со всеми числами, задающими движение
/// </summary>
[CreateAssetMenu(fileName = "PlayerMovementData", menuName = "ScriptableObjects/PlayerMovementData")]
public class PlayerMovementData : ScriptableObject
{
    [Header("Speed")] 
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float sprintAcceleration = 100f;
    public float walkAcceleration = 100f;
    public float turnSpeed = 8f;
    public float brakingForce = 4f; 
    
    [Header("Jump")] public float jumpForce = 10f;
    
    [Header("Sound")]
    public float walkStepInterval   = 0.5f;
    public float sprintStepInterval = 0.3f;

    [Header("Physics")] 
    public float groundCheckDistance = 0.15f;
    public LayerMask whatIsGround;
}