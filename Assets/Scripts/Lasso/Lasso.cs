using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class Lasso : NetworkBehaviour
{
    [Header("Settings")]
    public float throwSpeed = 35f;
    public float returnSpeed = 18f;
    public float pullSpeed = 12f;
    public float yankImpulseForce = 25f;
    public float maxDistance = 50f;

    private Rigidbody rb;
    private LassoController controller;

    public readonly SyncVar<NetworkObject> attachedNetObj = new SyncVar<NetworkObject>();
    public readonly SyncVar<bool> isFlying = new SyncVar<bool>();
    public readonly SyncVar<bool> isReturning = new SyncVar<bool>();
    public readonly SyncVar<bool> isLightObjectAttached = new SyncVar<bool>();
    public readonly SyncVar<bool> isHeavyMovable = new SyncVar<bool>();
    public readonly SyncVar<bool> isUnMovable = new SyncVar<bool>();

    private FixedJoint currentJoint;
    private float throwTime;
    private bool hasHit;
    private Vector3 hitPoint;
    public Vector3 HitPoint => hitPoint;

    public bool CanThrow => !isFlying.Value && !isReturning.Value && attachedNetObj.Value == null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponentInParent<LassoController>();
        attachedNetObj.OnChange += OnAttachedChanged;
    }

    private void OnAttachedChanged(NetworkObject oldValue, NetworkObject newValue, bool asServer)
    {
        if (currentJoint) Destroy(currentJoint);
        if (newValue != null && newValue.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            currentJoint = gameObject.AddComponent<FixedJoint>();
            currentJoint.connectedBody = targetRb;
            currentJoint.breakForce = Mathf.Infinity;
            currentJoint.enableCollision = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerThrow(Vector3 direction)
    {
        if (!CanThrow) return;

        hasHit = false;

        Vector3 startPos = controller.launchPoint != null
            ? controller.launchPoint.position
            : controller.cameraTransform.position + controller.cameraTransform.forward * 0.5f;

        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(direction);
        transform.SetParent(null);
        gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * throwSpeed;
        rb.angularVelocity = Vector3.zero;

        isFlying.Value = true;
        isReturning.Value = false;
        throwTime = Time.time;
        attachedNetObj.Value = null;
        isLightObjectAttached.Value = false;
        isHeavyMovable.Value = false;
        isUnMovable.Value = false;
        
        PlayerEvents.RaiseSuspicion();
    }

    public void ClientThrowPrediction(Vector3 startPosition, Vector3 direction)
    {
        if (!IsOwner) return;

        transform.position = startPosition;
        transform.rotation = Quaternion.LookRotation(direction);
        transform.SetParent(null);
        gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * throwSpeed;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !isFlying.Value || hasHit) return;
        TryAttach(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer || !isFlying.Value || hasHit) return;
        TryAttach(collision);
    }

    private void TryAttach(Collision collision)
    {
        if (collision.gameObject.CompareTag("Grabbable") &&
            collision.gameObject.TryGetComponent<Rigidbody>(out Rigidbody targetRb) &&
            collision.gameObject.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            hasHit = true;
            attachedNetObj.Value = netObj;
            isFlying.Value = false;

            isLightObjectAttached.Value = netObj.GetComponent<LightObject>() != null;
            isUnMovable.Value = netObj.GetComponent<UnMovable>() != null;
            isHeavyMovable.Value = netObj.GetComponent<HeavyMovable>() != null;

            rb.isKinematic = true;
            hitPoint = collision.contacts[0].point;
            transform.position = hitPoint;

            controller.OnLassoAttachedServer(netObj.gameObject);
        }
        else if (!hasHit)
        {
            hasHit = true;
            isReturning.Value = true;
            controller.OnLassoMiss();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (isFlying.Value && Vector3.Distance(transform.position, controller.transform.position) > maxDistance)
        {
            ServerStartReturn();
        }

        if (isReturning.Value)
        {
            Vector3 dir = controller.transform.position - transform.position;
            if (dir.sqrMagnitude < 2.25f)
            {
                ServerReturnToPlayer();
                return;
            }
            rb.linearVelocity = dir.normalized * returnSpeed;
        }
    }

    [ServerRpc]
    public void ServerPullTowardsPlayer()
    {
        if (attachedNetObj.Value == null) return;
        Vector3 dir = controller.transform.position - transform.position;
        rb.MovePosition(transform.position + dir.normalized * pullSpeed * Time.fixedDeltaTime);
    }

    [ServerRpc]
    public void ServerPullPlayerToTarget(NetworkObject target)
    {
        if (target == null || target != attachedNetObj.Value || !isUnMovable.Value) return;

        Vector3 dir = target.transform.position - controller.transform.position;
        if (dir.magnitude < 0.5f)
        {
            ServerYankAndDetach();
            return;
        }
        controller.GetComponent<Rigidbody>().linearVelocity = dir.normalized * pullSpeed;
    }

    [ServerRpc]
    public void ServerYankAndDetach()
    {
        if (attachedNetObj.Value == null) return;

        if (isLightObjectAttached.Value && attachedNetObj.Value.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            Vector3 dir = (controller.transform.position - attachedNetObj.Value.transform.position).normalized;
            targetRb.AddForce(dir * yankImpulseForce, ForceMode.Impulse);
        }

        attachedNetObj.Value = null;
        ServerReturnToPlayer();
    }

    [ServerRpc]
    public void ServerDetachAndReturn()
    {
        ServerReturnToPlayer();
    }

    [ServerRpc]
    public void ServerStartReturn()
    {
        isFlying.Value = false;
        isReturning.Value = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ServerReturnToPlayer()
    {
        attachedNetObj.Value = null;
        isFlying.Value = false;
        isReturning.Value = false;
        isLightObjectAttached.Value = false;
        isHeavyMovable.Value = false;
        isUnMovable.Value = false;
        hasHit = false;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.SetParent(controller.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(false);

        controller.OnLassoReturnedServer();
    }
}