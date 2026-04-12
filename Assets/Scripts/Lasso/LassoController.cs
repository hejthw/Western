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

    private LassoNetwork lasso;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform.Find("Joint/PlayerCamera")?.transform;

        lasso = GetComponentInChildren<LassoNetwork>();
    }

    private void Update()
    {
        if (!IsOwner || lasso == null) return;

        HandleThrow();
        HandlePull();
    }

    private void HandleThrow()
    {
        if (!Keyboard.current[throwKey].wasPressedThisFrame) return;

        Vector3 dir = cameraTransform.forward;
        Vector3 startPos = launchPoint != null
            ? launchPoint.position
            : cameraTransform.position + dir * 0.5f;

        if (lasso.CanThrow)
        {
            lasso.ServerThrow(startPos, dir, base.NetworkObject);
        }
        else
        {
            lasso.ServerDetachAndReturn();
            Invoke(nameof(ThrowAgain), 0.05f);
        }
        
        PlayerEvents.RaiseSuspicion();
        SoundBus.Play(SoundID.LassoThrow);
    }

    private void HandlePull()
    {
        if (!Keyboard.current[pullKey].wasPressedThisFrame) return;

        if (lasso.AttachedObject == null) return;

        lasso.ServerPullPressed();
    }

    private void ThrowAgain()
    {
        if (!IsOwner || lasso == null) return;

        Vector3 dir = cameraTransform.forward;
        Vector3 startPos = launchPoint != null
            ? launchPoint.position
            : cameraTransform.position + dir * 0.5f;

        lasso.ServerThrow(startPos, dir, base.NetworkObject);
    }

    public void OnLassoAttachedServer(GameObject target) { }
    public void OnLassoReturnedServer() { }
}