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
    private bool isAttached = false;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform.Find("Joint/PlayerCamera")?.transform;

        lasso = transform.Find("Lasso")?.gameObject;
        if (lasso == null) return;

        lassoScript = lasso.GetComponent<Lasso>();
        if (lassoScript == null)
            lassoScript = lasso.AddComponent<Lasso>();

        lasso.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current[throwKey].wasPressedThisFrame)
        {
            if (!isAttached)
                ThrowLasso();
            else
                ReturnLasso();
        }

        if (Keyboard.current[pullKey].isPressed && isAttached)
        {
            lassoScript.PullTowardsPlayer();
        }
    }

    private void ThrowLasso()
    {
        if (cameraTransform == null || lasso == null) return;

        Vector3 startPosition = launchPoint ? launchPoint.position : cameraTransform.position + cameraTransform.forward * 0.5f;
        Vector3 direction = cameraTransform.forward;

        lasso.transform.position = startPosition;
        lasso.transform.rotation = Quaternion.LookRotation(direction);

        lassoScript.Throw(direction);
        isAttached = false;
    }

    private void ReturnLasso()
    {
        if (lassoScript != null)
        {
            lassoScript.DetachAndReturn();
            isAttached = false;
        }
    }

    public void OnLassoAttached(GameObject target) { isAttached = true; }
    public void OnLassoMiss() { }
    public void OnLassoReturned() { isAttached = false; }
}