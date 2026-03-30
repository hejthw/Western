using UnityEngine;
using UnityEngine.InputSystem;

public class LassoController : MonoBehaviour
{
    [Header("Клавиши")]
    public Key throwKey = Key.F;
    public Key pullKey = Key.G;

    [Header("Настройки")]
    public Transform launchPoint;
    public Transform cameraTransform;

    private GameObject lasso;
    private Lasso lassoScript;
    private Rigidbody playerRigidbody;
    private PlayerController playerController;

    private bool isPullingToUnMovable = false;
    private bool isHanging = false;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform.Find("Joint/PlayerCamera")?.transform;

        lasso = transform.Find("Lasso")?.gameObject;
        if (lasso != null)
            lassoScript = lasso.GetComponent<Lasso>() ?? lasso.AddComponent<Lasso>();

        playerRigidbody = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (lassoScript == null) return;

        // Бросок / отцепление по F
        if (Keyboard.current[throwKey].wasPressedThisFrame)
        {
            if (!lassoScript.isAttached)
                ThrowLasso();
            else
            {
                if (isPullingToUnMovable || isHanging)
                {
                    isPullingToUnMovable = false;
                    isHanging = false;
                    DisablePlayerMovement(false);
                }
                lassoScript.DetachAndReturn();
            }
        }

        bool gPressed = Keyboard.current[pullKey].isPressed;
        bool gJustPressed = Keyboard.current[pullKey].wasPressedThisFrame;

        if (gPressed && lassoScript.isAttached)
        {
            if (lassoScript.isLightObjectAttached)
            {
                if (gJustPressed)
                    lassoScript.YankAndDetach();
            }
            else if (lassoScript.isUnMovable)
            {
                if (isHanging)
                {
                    isHanging = false;
                    isPullingToUnMovable = true;
                    DisablePlayerMovement(true);
                }
                else if (!isPullingToUnMovable)
                {
                    isPullingToUnMovable = true;
                    DisablePlayerMovement(true);
                }
                PullPlayerToTarget(lassoScript.attachedTarget);
            }
            else if (lassoScript.isHeavyMovable)
            {
                HeavyMovable heavy = lassoScript.attachedTarget.GetComponent<HeavyMovable>();
                if (heavy != null && lassoScript.isFrontHit)
                {
                    Vector3 globalDir = heavy.transform.TransformDirection(heavy.moveDirection);
                    heavy.ServerMove(globalDir, heavy.moveSpeed);
                }
            }
            else
            {
                lassoScript.PullTowardsPlayer();
            }
        }
        else if (!gPressed && (isPullingToUnMovable || isHanging))
        {
            if (isPullingToUnMovable)
            {
                isPullingToUnMovable = false;
                isHanging = true;
                if (playerRigidbody != null)
                    playerRigidbody.linearVelocity = Vector3.zero;
            }
        }
      
    }

    private void PullPlayerToTarget(GameObject target)
    {
        if (target == null || playerRigidbody == null) return;

        Vector3 direction = target.transform.position - transform.position;
        float distance = direction.magnitude;

        if (distance < 0.5f)
        {
            lassoScript.DetachAndReturn();
            isPullingToUnMovable = false;
            isHanging = false;
            DisablePlayerMovement(false);
            return;
        }

        Vector3 targetVelocity = direction.normalized * lassoScript.pullSpeed;
        playerRigidbody.linearVelocity = targetVelocity;
    }

    private void ThrowLasso()
    {
        if (cameraTransform == null || lasso == null) return;

        Vector3 startPosition = launchPoint ? launchPoint.position : cameraTransform.position + cameraTransform.forward * 0.5f;
        Vector3 direction = cameraTransform.forward;

        lasso.transform.position = startPosition;
        lasso.transform.rotation = Quaternion.LookRotation(direction);

        lassoScript.Throw(direction);
    }

    private void DisablePlayerMovement(bool disable)
    {
        if (playerController != null)
        {
            if (disable)
                playerController.DisableMovement();
            else
                playerController.EnableMovement();
        }
    }

    public void OnLassoAttached(GameObject target) { }
    public void OnLassoMiss() { }
    public void OnLassoReturned()
    {
        if (isPullingToUnMovable || isHanging)
        {
            isPullingToUnMovable = false;
            isHanging = false;
            DisablePlayerMovement(false);
        }
    }
}