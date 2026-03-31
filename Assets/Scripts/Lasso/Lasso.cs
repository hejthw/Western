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
        if (attachedNetObj.Value != null) return;

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
    }

    [ServerRpc]
    public void ServerStartReturn()
    {
        if (attachedNetObj.Value != null)
        {
            ServerDetachAndReturn();
            return;
        }
        if (isReturning.Value) return;
        isFlying.Value = false;
        isReturning.Value = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (!isFlying.Value) return;
        if (Time.time - throwTime < 0.25f) return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (collision.gameObject.CompareTag("Grabbable") &&
            collision.gameObject.TryGetComponent<Rigidbody>(out Rigidbody targetRb) &&
            collision.gameObject.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            attachedNetObj.Value = netObj;
            isFlying.Value = false;

            isLightObjectAttached.Value = netObj.GetComponent<LightObject>() != null;
            isUnMovable.Value = netObj.GetComponent<UnMovable>() != null;

            var heavy = netObj.GetComponent<HeavyMovable>();
            isHeavyMovable.Value = heavy != null;

            rb.isKinematic = true;
            transform.position = collision.contacts[0].point;

            controller.OnLassoAttachedServer(netObj.gameObject);
        }
        else
        {
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
        Vector3 dirToPlayer = controller.transform.position - transform.position;
        rb.MovePosition(transform.position + dirToPlayer.normalized * pullSpeed * Time.fixedDeltaTime);
    }

    [ServerRpc]
    public void ServerPullPlayerToTarget(NetworkObject target)   
    {
        if (target == null || target != attachedNetObj.Value) return;
        if (!isUnMovable.Value) return;

        Vector3 dir = target.transform.position - controller.transform.position;
        float dist = dir.magnitude;
        if (dist < 0.5f)
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
            Vector3 yankDir = (controller.transform.position - attachedNetObj.Value.transform.position).normalized;
            targetRb.AddForce(yankDir * yankImpulseForce, ForceMode.Impulse);
        }

        attachedNetObj.Value = null;
        ServerReturnToPlayer();
    }

    [ServerRpc]
    public void ServerDetachAndReturn()
    {
        if (attachedNetObj.Value == null)
        {
            ServerStartReturn();
            return;
        }

        if (isLightObjectAttached.Value)
            ServerYankAndDetach();
        else if (attachedNetObj.Value.TryGetComponent<HeavyMovable>(out HeavyMovable heavy))
            heavy.ResetPullCount();

        ServerReturnToPlayer();
    }

    private void ServerReturnToPlayer()
    {
        attachedNetObj.Value = null;
        isFlying.Value = false;
        isReturning.Value = false;
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