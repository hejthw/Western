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
    public bool IsHoldingFinish { get; private set; }

    public bool isDead { get ; private set ; }
    
    public event Action JumpPressedEvent;
    public event Action OnSprintEvent;
    public event Action OnTestEvent;
    public event Action OnAttackEvent;
    public event Action OnPickupEvent;
    public event Action OnDropEvent;
    public event Action<int> OnSlotKeyPressed;
    public event Action OnLassoPullStarted;   
    public event Action OnLassoPullEnded;

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

    public void OnAttack(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
        {
            Debug.Log("[PlayerInput] OnAttack triggered");
            if (isDead)
                PlayerEvents.RaisePrevTargetEvent();
            else
                OnAttackEvent?.Invoke();
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
    public void OnSlot1(InputValue value)
    {
        if (value.Get<float>() > 0.5f) OnSlotKeyPressed?.Invoke(0);
    }

    public void OnSlot2(InputValue value)
    {
        if (value.Get<float>() > 0.5f) OnSlotKeyPressed?.Invoke(1);
    }

    public void OnSlot3(InputValue value)
    {
        if (value.Get<float>() > 0.5f) OnSlotKeyPressed?.Invoke(2);
    }
    public void OnLassoPull(InputValue value)
    {
        Debug.Log($"[PlayerInput] OnLassoPull: value={value.Get<float>()}");
        if (value.Get<float>() > 0.4f)
            OnLassoPullStarted?.Invoke();
        else
            OnLassoPullEnded?.Invoke();
    }
    public void OnFinish(InputValue value)
    {
        IsHoldingFinish = value.Get<float>() > 0.5f;
    }
}
