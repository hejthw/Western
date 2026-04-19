using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class LassoController : NetworkBehaviour
{
    [Header("Keys")]
    public Key throwKey = Key.F;
    public Key pullKey = Key.G;
    public Key jumpOffKey = Key.Space;

    [Header("References")]
    public Transform launchPoint;
    public Transform cameraTransform;
    
    [Header("Network Sync")]
    [SerializeField] private float pullInputSyncInterval = 0.05f;

    private LassoNetwork lasso;
    private bool pullHeldLastFrame;
    private float _pullInputSyncTimer;

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
        HandlePullHold();
        HandleJumpOff();
        SyncPullHoldHeartbeat();
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

    private void HandlePullHold()
    {
        bool held = Keyboard.current[pullKey].isPressed;
        if (held == pullHeldLastFrame) return;
        
        pullHeldLastFrame = held;

        if (lasso.AttachedObject == null) return;

        if (held)
            lasso.ServerPullPressed();
        else
            lasso.ServerSetPullHeld(false);
    }
    
    private void SyncPullHoldHeartbeat()
    {
        if (lasso.AttachedObject == null) return;
        
        _pullInputSyncTimer -= Time.deltaTime;
        if (_pullInputSyncTimer > 0f) return;
        _pullInputSyncTimer = pullInputSyncInterval;
        
        bool held = Keyboard.current[pullKey].isPressed;
        lasso.ServerSetPullHeld(held);
    }
    
    private void HandleJumpOff()
    {
        if (!Keyboard.current[jumpOffKey].wasPressedThisFrame) return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.ActiveRopeClimb != null)
        {
            pc.RequestRopeJumpOff(pc.ActiveRopeClimb);
            return;
        }

        if (!lasso.IsPlayerPulling) return;
        lasso.ServerJumpOffPull();
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