using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class Lasso : NetworkBehaviour
{
    [Header("Настройки")]
    public float throwSpeed = 35f;
    public float returnSpeed = 18f;
    public float pullSpeed = 12f;

    private Rigidbody rb;
    private LassoController controller;

    private bool isFlying;
    private bool isReturning;
    private GameObject attachedTarget;
    private FixedJoint currentJoint;
    private float throwTime;

    public readonly SyncVar<bool> isAttached = new SyncVar<bool>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponentInParent<LassoController>();
    }

    public void Throw(Vector3 direction)
    {
        if (!IsOwner) return;

        transform.SetParent(null);
        gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * throwSpeed;
        rb.angularVelocity = Vector3.zero;

        isFlying = true;
        throwTime = Time.time;
        isAttached.Value = false;
        isReturning = false;
        attachedTarget = null;

        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner || !isFlying) return;
        if (Time.time - throwTime < 0.25f) return;

        isFlying = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (collision.gameObject.CompareTag("Grabbable") &&
            collision.gameObject.TryGetComponent(out Rigidbody targetRb))
        {
            attachedTarget = collision.gameObject;
            isAttached.Value = true;

            rb.isKinematic = true;
            transform.position = collision.contacts[0].point;

            currentJoint = gameObject.AddComponent<FixedJoint>();
            currentJoint.connectedBody = targetRb;
            currentJoint.breakForce = Mathf.Infinity;

            controller.OnLassoAttached(attachedTarget);
        }
        else
        {
            isReturning = true;
            controller.OnLassoMiss();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (isReturning)
        {
            Vector3 dir = controller.transform.position - transform.position;
            if (dir.sqrMagnitude < 2.25f)
            {
                ReturnToPlayer();
                return;
            }
            rb.linearVelocity = dir.normalized * returnSpeed;
        }
        else if (isAttached.Value && Keyboard.current[Key.G].isPressed)
        {
            PullTowardsPlayer();
        }
    }

    public void PullTowardsPlayer()
    {
        if (!isAttached.Value || attachedTarget == null || !rb.isKinematic) return;

        Vector3 dir = controller.transform.position - transform.position;
        rb.MovePosition(transform.position + dir.normalized * pullSpeed * Time.fixedDeltaTime);
    }

    private void ReturnToPlayer()
    {
        isReturning = false;
        isAttached.Value = false;

        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
        }

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        transform.SetParent(controller.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        gameObject.SetActive(false);
        controller.OnLassoReturned();
    }

    public void DetachAndReturn()
    {
        if (!isAttached.Value) return;

        isAttached.Value = false;
        isReturning = true;

        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
        }
    }
}