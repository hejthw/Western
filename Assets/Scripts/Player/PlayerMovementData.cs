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
    
    [Header("Jump")] public float jumpForce = 10f;

    [Header("Physics")] 
    public float groundCheckDistance = 0.15f;
    public LayerMask whatIsGround;
}