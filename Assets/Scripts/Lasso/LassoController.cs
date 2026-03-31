using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class LassoController : NetworkBehaviour
{
    [Header("Keys")]
    public Key throwKey = Key.F;
    public Key pullKey = Key.G;

    [Header("References")]
    public Transform launchPoint;
    public Transform cameraTransform;

    private Lasso lasso;
    private Rigidbody playerRb;
    private PlayerController playerController;
    private PlayerInput playerInput;

    private bool pullingToUnMovable;
    private bool hanging;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform.Find("Joint/PlayerCamera")?.transform;

        var lassoObj = transform.Find("Lasso")?.gameObject;
        if (lassoObj != null)
            lasso = lassoObj.GetComponent<Lasso>();

        playerRb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.JumpPressedEvent += OnJumpPressed;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (lasso == null) return;

        if (Keyboard.current[throwKey].wasPressedThisFrame)
        {
            if (lasso.isFlying.Value || lasso.attachedNetObj.Value != null)
            {
                lasso.ServerDetachAndReturn();
            }
            else
            {
                ThrowLasso();
            }
        }

        bool gPressed = Keyboard.current[pullKey].isPressed;
        bool gJustPressed = Keyboard.current[pullKey].wasPressedThisFrame;

        if (gPressed && lasso.attachedNetObj.Value != null)
        {
            if (lasso.isLightObjectAttached.Value)
            {
                if (gJustPressed)
                    lasso.ServerYankAndDetach();
            }
            else if (lasso.isUnMovable.Value)
            {
            
                if (hanging)
                {
                    hanging = false;
                    pullingToUnMovable = true;
                    DisableMovement();
                }
                else if (!pullingToUnMovable && !hanging)
                {
                    pullingToUnMovable = true;
                    DisableMovement();
                }
                lasso.ServerPullPlayerToTarget(lasso.attachedNetObj.Value);
            }
            else if (lasso.isHeavyMovable.Value)
            {
                var heavy = lasso.attachedNetObj.Value.GetComponent<HeavyMovable>();
                if (heavy != null && lasso.isFrontHit.Value)
                {
                    Vector3 globalDir = heavy.transform.TransformDirection(heavy.moveDirection);
                    lasso.ServerMoveHeavy(heavy, globalDir, heavy.moveSpeed);
                }
            }
            else
            {
                lasso.ServerPullTowardsPlayer();
            }
        }
        else if (!gPressed && (pullingToUnMovable || hanging))
        {
            if (pullingToUnMovable)
            {
                pullingToUnMovable = false;
                hanging = true;
                playerRb.linearVelocity = Vector3.zero;
            }
        }
    }

    private void FixedUpdate()
    {
        if (hanging && !pullingToUnMovable)
        {
            Vector3 vel = playerRb.linearVelocity;
            vel.x = 0f;
            vel.z = 0f;
            playerRb.linearVelocity = vel;
        }
    }

    private void ThrowLasso()
    {
        if (cameraTransform == null) return;
        Vector3 startPos = launchPoint != null ? launchPoint.position : cameraTransform.position + cameraTransform.forward * 0.5f;
        Vector3 direction = cameraTransform.forward;
        lasso.transform.position = startPos;
        lasso.transform.rotation = Quaternion.LookRotation(direction);
        lasso.ServerThrow(direction);
    }

    private void DisableMovement() => playerController?.DisableMovement();
    private void EnableMovement() => playerController?.EnableMovement();

    public void OnLassoAttachedServer(GameObject target) { }
    public void OnLassoMiss() { }
    public void OnLassoReturnedServer()
    {
        pullingToUnMovable = false;
        hanging = false;
        EnableMovement();
    }

    private void OnDestroy()
    {
        if (playerInput != null)
            playerInput.JumpPressedEvent -= OnJumpPressed;
    }

    private void OnJumpPressed()
    {
        if (!IsOwner) return;
        if (pullingToUnMovable || hanging)
        {
            lasso.ServerDetachAndReturn();
            if (lasso.attachedNetObj.Value != null)
            {
                Vector3 awayDir = (transform.position - lasso.attachedNetObj.Value.transform.position).normalized;
                awayDir.y = 0.5f;
                playerRb.linearVelocity = awayDir * 5f;
            }
            pullingToUnMovable = false;
            hanging = false;
            EnableMovement();
        }
    }
}