using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.InputSystem.Controls;

public class PlayerController : MonoBehaviour
{
    #region Variables: Speed
    public float walkSpeed;
    public float sprintSpeed;
    [SerializeField] private float currentSpeed;
    #endregion
    
    #region Variables: Dependecies
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    #endregion
    
    #region Variables: Gravity
    private const float Gravity = -9.81f;
    [SerializeField] private float _gravityMultiplier = 3.0f;
    [SerializeField] private float _velocity;
    private Vector3 gravityVector = new Vector3(0, 0, 0);
    #endregion
    
    private Vector2 _move;
    
    private void Update()
    {
        ApplyGravity();
        characterController.Move((GetForward() * _move.y + GetRight() * _move.x + gravityVector) 
                                 * (currentSpeed * Time.deltaTime));
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && _velocity < 0.0f)
        {
            _velocity = -1;
        }
        else
        {
            _velocity += Gravity * _gravityMultiplier * Time.deltaTime;
        }

        gravityVector.y = _velocity;
    }

    private void Start()
    {
        currentSpeed = walkSpeed;
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void OnMove(InputValue value)
    {
        _move = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        if (value.Get<float>() > 0.5f)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
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
