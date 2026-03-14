using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Получение input'a из компонента PlayerInput
/// </summary>

public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveInput {get ; private set;}
    
    public bool SprintHeld { get; private set; }
    
    public event Action JumpPressedEvent;
    public event Action OnSprintEvent;

    public void OnMove(InputValue value) => MoveInput = value.Get<Vector2>();

    public void OnSprint(InputValue value)
    {
        SprintHeld = value.Get<float>() > 0.5; 
        OnSprintEvent?.Invoke();
    }

    public void OnJump(InputValue value)
    {
        if (value.Get<float>() > 0.5f) JumpPressedEvent?.Invoke();
    }
}
