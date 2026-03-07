using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;

public class PlayerController : NetworkBehaviour
{
    #region Variables: Dependecies
    [Header("Dependecies")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    #endregion
    
    #region Variables: Movement
    [Header("Movement")]
    [SerializeField] private float currentSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float jumpPower;
    public float walkToSprintTranstitionSpeed = 75f;
    public float SprintToWalkTranstitionSpeed = 75f;
    #endregion
    
    #region Variables: Gravity
    [Header("Gravity")]
    private const float Gravity = -9.81f;
    [SerializeField] private float _gravityMultiplier = 3.0f;
    [SerializeField] private float _velocity;
    private Vector3 gravityVector = new Vector3(0, 0, 0);
    #endregion


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (base.IsOwner)
        {
            // Активируем камеру только для владельца
            cinemachineCamera.gameObject.SetActive(true);
        }
        else
        {
            // Отключаем скрипт и камеру для чужих персонажей
            cinemachineCamera.gameObject.SetActive(false);
            enabled = false;
        }
    }
    
    [SerializeField] private PlayerState playerState =  PlayerState.STATE_IDLE;
    
    public PlayerState GetPlayerState => playerState;
    
    private Vector2 _move;
    
    private void Start()
    {
        currentSpeed = 5;
    }
    
    private void Update()
    {
        ApplyGravity();
        ApplyMovement();
        UpdatePlayerState();
    }
    
    public void OnMove(InputValue value)
    {
        _move = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
            StartCoroutine(
                LerpSprint(walkSpeed,sprintSpeed,walkToSprintTranstitionSpeed));
        else 
            StartCoroutine(
                LerpSprint(sprintSpeed,walkSpeed,SprintToWalkTranstitionSpeed));
        
        // currentSpeed = value.Get<float>() > 0.5f ? sprintSpeed : walkSpeed;
        PlayerEvent.OnSprint.Invoke();
    }
    
    private void ApplyMovement()
    {
        var move = (GetForward() * _move.y + GetRight() * _move.x)
                   * currentSpeed;
        move += gravityVector;
        characterController.Move(move * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && _velocity < 0.0f)
        {
            _velocity = -1;
        }
        else if (_velocity <= 25.0f)
        {
            _velocity += Gravity * _gravityMultiplier * Time.deltaTime;
        }

        gravityVector.y = _velocity;
    }

    private void UpdatePlayerState()
    {
        if (_move is { x: 0, y: 0 })
            playerState = PlayerState.STATE_IDLE;
        else if (currentSpeed >= walkSpeed + 0.01f)
            playerState = PlayerState.STATE_SPRINT;
        else 
            playerState = PlayerState.STATE_WALK;
        
        if (_velocity > -1.0f)
            playerState = PlayerState.STATE_JUMP;
        else if (_velocity < -1.0f)
            playerState = PlayerState.STATE_FALL;
    }
    
    private IEnumerator LerpSprint(float startSpeed, float endSpeed, float lerpDuration)
    {
        float speed = 1 / lerpDuration;

        for (float i = 0; i < 1.0f; i += speed)
        {
            var temp = Mathf.Lerp(startSpeed, endSpeed, i);
            currentSpeed = temp;
            yield return null;
        }
    }

    public void OnJump()
    {
        if (characterController.isGrounded)
        {
            _velocity += jumpPower;
        }
    }

    private Vector3 GetForward()
    {
        Vector3 forward = cinemachineCamera.transform.forward;
        forward.y = 0;

        return forward.normalized;
    }

    private Vector3 GetRight()
    {
        Vector3 right = cinemachineCamera.transform.right;
        right.y = 0;
        
        return right.normalized;
    }
}
