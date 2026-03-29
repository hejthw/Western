using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Получение input'a из компонента PlayerInput
/// </summary>

public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveInput {get ; private set;}
    
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get ; private set;}
    
    public bool isKnockout { get ; private set ; }

    private void OnEnable()
    {
        PlayerEvents.OnKnockoutEvent += bred;
    }

    private void OnDisable()
    {
        PlayerEvents.OnKnockoutEvent -= bred;
    }

    private void bred(bool a)
    {
        isKnockout = a;
    }
    
    public event Action JumpPressedEvent;
    public event Action OnSprintEvent;
    public event Action OnTestEvent;
    public event Action OnCrouchEvent;
    public event Action OnAttackEvent;
    public event Action OnPickupEvent;
    public event Action OnDropEvent;

    public void OnMove(InputValue value) => MoveInput = value.Get<Vector2>();

    public void OnSprint(InputValue value)
    {
        SprintHeld = value.Get<float>() > 0.5; 
        OnSprintEvent?.Invoke();
    }
    
    public void OnCrouch(InputValue value)
    {
        CrouchHeld = value.Get<float>() > 0.5;
        OnCrouchEvent?.Invoke();
    }

    public void OnJump(InputValue value)
    {
        if (value.Get<float>() > 0.5f) JumpPressedEvent?.Invoke();
    }

    public void OnAttack(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
        {
            if (isKnockout)
                PlayerEvents.RaisePrevTargetEvent();
            else OnAttackEvent?.Invoke();
        }
    }
    
    public void OnAim(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
        {
            PlayerEvents.RaiseNextTargetEvent();
        }
    }

    public bool IsMoving() => MoveInput != Vector2.zero;
    
    public void OnTestAction(InputValue value)
    {
        OnTestEvent?.Invoke();
    }
    
    public void OnPickup(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
            OnPickupEvent?.Invoke();
    }

    public void OnDrop(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
            OnDropEvent?.Invoke();
    }
}
