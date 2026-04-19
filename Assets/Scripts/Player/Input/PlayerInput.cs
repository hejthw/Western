using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Подписка на действия <see cref="UnityEngine.InputSystem.PlayerInput"/> (тип <b>Value</b>: Move/Sprint
/// через <c>performed</c>/<c>canceled</c>) — иначе SendMessage может не обновлять WASD.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool IsHoldingFinish { get; private set; }

    public bool isDead { get; private set; }

    public event Action JumpPressedEvent;
    public event Action OnSprintEvent;
    public event Action OnTestEvent;
    public event Action OnAttackEvent;
    public event Action OnPickupEvent;
    public event Action OnDropEvent;
    public event Action<int> OnSlotKeyPressed;
    public event Action OnLassoPullStarted;
    public event Action OnLassoPullEnded;

    private UnityEngine.InputSystem.PlayerInput _systemPlayerInput;

    private InputAction _move;
    private InputAction _sprint;
    private InputAction _jump;
    private InputAction _attack;
    private InputAction _aim;
    private InputAction _testAction;
    private InputAction _interact;
    private InputAction _pickup;
    private InputAction _drop;
    private InputAction _slot1;
    private InputAction _slot2;
    private InputAction _slot3;
    private InputAction _lassoPull;
    private InputAction _finish;

    private bool _bound;

    private void Start()
    {
        _systemPlayerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (_systemPlayerInput == null || _systemPlayerInput.actions == null)
            return;

        InputActionMap map = _systemPlayerInput.actions.FindActionMap("Player", throwIfNotFound: false);
        if (map == null)
            return;

        _move = map.FindAction("Move", throwIfNotFound: false);
        if (_move != null)
        {
            _move.performed += MovePerformed;
            _move.canceled += MoveCanceled;
        }

        _sprint = map.FindAction("Sprint", throwIfNotFound: false);
        if (_sprint != null)
        {
            _sprint.performed += SprintPerformed;
            _sprint.canceled += SprintCanceled;
        }

        _jump = map.FindAction("Jump", throwIfNotFound: false);
        if (_jump != null)
            _jump.performed += JumpPerformed;

        _attack = map.FindAction("Attack", throwIfNotFound: false);
        if (_attack != null)
            _attack.performed += AttackPerformed;

        _aim = map.FindAction("Aim", throwIfNotFound: false);
        if (_aim != null)
            _aim.performed += AimPerformed;

        _testAction = map.FindAction("TestAction", throwIfNotFound: false);
        if (_testAction != null)
            _testAction.performed += TestActionPerformed;

        _interact = map.FindAction("Interact", throwIfNotFound: false);
        if (_interact != null)
            _interact.performed += InteractPerformed;

        _pickup = map.FindAction("Pickup", throwIfNotFound: false);
        if (_pickup != null)
            _pickup.performed += PickupPerformed;

        _drop = map.FindAction("Drop", throwIfNotFound: false);
        if (_drop != null)
            _drop.performed += DropPerformed;

        _slot1 = map.FindAction("Slot1", throwIfNotFound: false);
        if (_slot1 != null)
            _slot1.performed += Slot1Performed;

        _slot2 = map.FindAction("Slot2", throwIfNotFound: false);
        if (_slot2 != null)
            _slot2.performed += Slot2Performed;

        _slot3 = map.FindAction("Slot3", throwIfNotFound: false);
        if (_slot3 != null)
            _slot3.performed += Slot3Performed;

        _lassoPull = map.FindAction("LassoPull", throwIfNotFound: false);
        if (_lassoPull != null)
        {
            _lassoPull.performed += LassoPullPerformed;
            _lassoPull.canceled += LassoPullCanceled;
        }

        _finish = map.FindAction("Finish", throwIfNotFound: false);
        if (_finish != null)
        {
            _finish.performed += FinishPerformed;
            _finish.canceled += FinishCanceled;
        }

        _bound = true;
    }

    private void OnDisable()
    {
        if (!_bound)
            return;

        if (_move != null)
        {
            _move.performed -= MovePerformed;
            _move.canceled -= MoveCanceled;
            _move = null;
        }

        if (_sprint != null)
        {
            _sprint.performed -= SprintPerformed;
            _sprint.canceled -= SprintCanceled;
            _sprint = null;
        }

        if (_jump != null)
        {
            _jump.performed -= JumpPerformed;
            _jump = null;
        }

        if (_attack != null)
        {
            _attack.performed -= AttackPerformed;
            _attack = null;
        }

        if (_aim != null)
        {
            _aim.performed -= AimPerformed;
            _aim = null;
        }

        if (_testAction != null)
        {
            _testAction.performed -= TestActionPerformed;
            _testAction = null;
        }

        if (_interact != null)
        {
            _interact.performed -= InteractPerformed;
            _interact = null;
        }

        if (_pickup != null)
        {
            _pickup.performed -= PickupPerformed;
            _pickup = null;
        }

        if (_drop != null)
        {
            _drop.performed -= DropPerformed;
            _drop = null;
        }

        if (_slot1 != null)
        {
            _slot1.performed -= Slot1Performed;
            _slot1 = null;
        }

        if (_slot2 != null)
        {
            _slot2.performed -= Slot2Performed;
            _slot2 = null;
        }

        if (_slot3 != null)
        {
            _slot3.performed -= Slot3Performed;
            _slot3 = null;
        }

        if (_lassoPull != null)
        {
            _lassoPull.performed -= LassoPullPerformed;
            _lassoPull.canceled -= LassoPullCanceled;
            _lassoPull = null;
        }

        if (_finish != null)
        {
            _finish.performed -= FinishPerformed;
            _finish.canceled -= FinishCanceled;
            _finish = null;
        }

        _systemPlayerInput = null;
        _bound = false;
    }

    private void MovePerformed(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }

    private void MoveCanceled(InputAction.CallbackContext ctx)
    {
        MoveInput = Vector2.zero;
    }

    private void SprintPerformed(InputAction.CallbackContext ctx)
    {
        SprintHeld = ctx.ReadValue<float>() > 0.5f;
        OnSprintEvent?.Invoke();
    }

    private void SprintCanceled(InputAction.CallbackContext ctx)
    {
        SprintHeld = false;
        OnSprintEvent?.Invoke();
    }

    private void JumpPerformed(InputAction.CallbackContext ctx)
    {
        JumpPressedEvent?.Invoke();
    }

    private void AttackPerformed(InputAction.CallbackContext ctx)
    {
        if (isDead)
            PlayerEvents.RaisePrevTargetEvent();
        else
            OnAttackEvent?.Invoke();
    }

    private void AimPerformed(InputAction.CallbackContext ctx)
    {
        PlayerEvents.RaiseNextTargetEvent();
    }

    public bool IsMoving() => MoveInput != Vector2.zero;

    private void TestActionPerformed(InputAction.CallbackContext ctx)
    {
        OnTestEvent?.Invoke();
    }

    private void InteractPerformed(InputAction.CallbackContext ctx)
    {
        OnPickupEvent?.Invoke();
    }

    private void PickupPerformed(InputAction.CallbackContext ctx)
    {
        OnPickupEvent?.Invoke();
    }

    private void DropPerformed(InputAction.CallbackContext ctx)
    {
        OnDropEvent?.Invoke();
    }

    private void Slot1Performed(InputAction.CallbackContext ctx) => OnSlotKeyPressed?.Invoke(0);
    private void Slot2Performed(InputAction.CallbackContext ctx) => OnSlotKeyPressed?.Invoke(1);
    private void Slot3Performed(InputAction.CallbackContext ctx) => OnSlotKeyPressed?.Invoke(2);

    private void LassoPullPerformed(InputAction.CallbackContext ctx)
    {
        OnLassoPullStarted?.Invoke();
    }

    private void LassoPullCanceled(InputAction.CallbackContext ctx)
    {
        OnLassoPullEnded?.Invoke();
    }

    private void FinishPerformed(InputAction.CallbackContext ctx)
    {
        IsHoldingFinish = ctx.ReadValue<float>() > 0.5f;
    }

    private void FinishCanceled(InputAction.CallbackContext ctx)
    {
        IsHoldingFinish = false;
    }
}
