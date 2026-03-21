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

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform.Find("Joint/PlayerCamera")?.transform;

        lasso = transform.Find("Lasso")?.gameObject;
        if (lasso != null)
            lassoScript = lasso.GetComponent<Lasso>() ?? lasso.AddComponent<Lasso>();
    }

    void Update()
    {
        if (lassoScript == null) return;

        if (Keyboard.current[throwKey].wasPressedThisFrame)
        {
            if (!lassoScript.isAttached)
                ThrowLasso();
            else
                lassoScript.DetachAndReturn();
        }

        // Hold G — притягивание (только для обычных объектов)
        if (Keyboard.current[pullKey].isPressed && lassoScript.isAttached && !lassoScript.isLightObjectAttached)
        {
            lassoScript.PullTowardsPlayer();
        }

        // Одиночное нажатие G — дёрг для LightObject
        if (Keyboard.current[pullKey].wasPressedThisFrame && lassoScript.isAttached && lassoScript.isLightObjectAttached)
        {
            lassoScript.YankAndDetach();
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
    }

    public void OnLassoAttached(GameObject target) { }
    public void OnLassoMiss() { }
    public void OnLassoReturned() { }
}