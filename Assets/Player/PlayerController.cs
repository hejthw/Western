using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed;
    public float sprintSpeed;
    
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float currentSpeed;
    
    private Vector2 _move;
    
    private void Update()
    {
        characterController.Move((GetForward() * _move.y + GetRight() * _move.x) * (Time.deltaTime * currentSpeed));
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
