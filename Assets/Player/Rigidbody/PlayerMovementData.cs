using UnityEngine;

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
    public float groundDrag = 5f;
    public float airDrag = 5f;
    public float groundCheckDistance = 0.15f;
    public LayerMask whatIsGround;
}