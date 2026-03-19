using UnityEngine;
using System;
using System.Collections;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private PlayerMovementData _data;

    private PlayerInput _input;
    private Coroutine _staminaCoroutine;
    
    public event Action OnStaminaEmpty;
    
    public float Current { get; private set; }
    public bool IsEmpty => Current <= 0f;

    void Awake()
    {
        _input = GetComponent<PlayerInput>();
        Current = _data.maxStamina;
    }

    void OnEnable()  => _input.OnSprintEvent += OnSprintChanged;
    void OnDisable() => _input.OnSprintEvent -= OnSprintChanged;

    private void OnSprintChanged()
    {
        if (_staminaCoroutine != null)
            StopCoroutine(_staminaCoroutine);

        _staminaCoroutine = _input.SprintHeld
            ? StartCoroutine(DrainRoutine())
            : StartCoroutine(RegenRoutine());
    }
    
    private IEnumerator DrainRoutine()
    {
        while (_input.SprintHeld && !_input.CrouchHeld && Current > 0f && _input.IsMoving())
        {
            Current = Mathf.Max(Current - _data.drainPerSecond * Time.deltaTime, 0f);
            PlayerEvents.RaiseStaminaChange(Current);
            yield return null;
        }
        
        if (Current <= 0f)
        {
            OnStaminaEmpty?.Invoke();
            _staminaCoroutine = StartCoroutine(RegenRoutine());
        }
    }
    
    private IEnumerator RegenRoutine()
    {
        yield return new WaitForSeconds(_data.regenDelay);

        while (!_input.SprintHeld && Current < _data.maxStamina)
        {
            Current = Mathf.Min(Current + _data.regenPerSecond * Time.deltaTime, _data.maxStamina);
            PlayerEvents.RaiseStaminaChange(Current);
            yield return null;
        }
    }
}