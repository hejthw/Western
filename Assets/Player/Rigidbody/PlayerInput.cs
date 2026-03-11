using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputBridge : MonoBehaviour
{
    public Vector2 MoveInput {get ; private set;}
    
    public bool SprintHeld { get; private set; }
    
    public bool JumpPressed { get; private set; }

    public void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        SprintHeld = value.Get<float>() > 0.5f;
    }

    public void OnJump(InputValue value)
    {
        JumpPressed = value.Get<float>() > 0.5f;
    }
    
    public void ConsumeJump() 
    {
        JumpPressed = false;
    }
}