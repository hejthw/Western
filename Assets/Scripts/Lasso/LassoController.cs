using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;

public class LassoController : NetworkBehaviour
{
    [Header("Клавиши")]
    public Key throwKey = Key.F;
    public Key pullKey = Key.G;

    [Header("Настройки")]
    public Transform launchPoint;
    public Transform cameraTransform;

    private GameObject lasso;
    private Lasso lassoScript;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform.Find("Joint/PlayerCamera")?.transform;

        lasso = transform.Find("Lasso")?.gameObject;
        if (lasso != null)
            lassoScript = lasso.GetComponent<Lasso>() ?? lasso.AddComponent<Lasso>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current[throwKey].wasPressedThisFrame)
        {
            if (!lassoScript.isAttached.Value)
                ThrowLasso();
            else
                ReturnLasso();
        }

        if (Keyboard.current[pullKey].isPressed && lassoScript.isAttached.Value)
            lassoScript.PullTowardsPlayer();
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

    private void ReturnLasso()
    {
        lassoScript?.DetachAndReturn();
    }

    public void OnLassoAttached(GameObject target) { }
    public void OnLassoMiss() { }
    public void OnLassoReturned() { }
}