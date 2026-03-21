using UnityEngine;
using UnityEngine.InputSystem;

public class Lasso : MonoBehaviour
{
    [Header("Настройки")]
    public float throwSpeed = 35f;
    public float returnSpeed = 18f;
    public float pullSpeed = 12f;
    public float yankImpulseForce = 25f;

    private Rigidbody rb;
    private LassoController controller;

    private bool isFlying;
    private bool isReturning;
    private GameObject attachedTarget;

    // ← Эти два поля теперь public
    public bool isAttached;
    public bool isLightObjectAttached;

    private FixedJoint currentJoint;
    private float throwTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponentInParent<LassoController>();
    }

    public void Throw(Vector3 direction)
    {
        transform.SetParent(null);
        gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.linearVelocity = direction.normalized * throwSpeed;
        rb.angularVelocity = Vector3.zero;

        isFlying = true;
        throwTime = Time.time;
        isAttached = false;
        isReturning = false;
        attachedTarget = null;
        isLightObjectAttached = false;

        if (currentJoint != null) Destroy(currentJoint);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isFlying) return;
        if (Time.time - throwTime < 0.25f) return;

        isFlying = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (collision.gameObject.CompareTag("Grabbable") &&
            collision.gameObject.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            attachedTarget = collision.gameObject;
            isAttached = true;
            isLightObjectAttached = attachedTarget.GetComponent<LightObject>() != null;

            rb.isKinematic = true;
            transform.position = collision.contacts[0].point;

            currentJoint = gameObject.AddComponent<FixedJoint>();
            currentJoint.connectedBody = targetRb;
            currentJoint.breakForce = Mathf.Infinity;
            currentJoint.enableCollision = false;

            controller.OnLassoAttached(attachedTarget);
        }
        else
        {
            isReturning = true;
            controller.OnLassoMiss();
        }
    }

    void FixedUpdate()
    {
        if (isReturning)
        {
            Vector3 dirToPlayer = controller.transform.position - transform.position;
            if (dirToPlayer.sqrMagnitude < 2.25f)
            {
                ReturnToPlayer();
                return;
            }
            rb.linearVelocity = dirToPlayer.normalized * returnSpeed;
        }
        else if (isAttached && !isLightObjectAttached && Keyboard.current[Key.G].isPressed)
        {
            PullTowardsPlayer();
        }
    }

    public void PullTowardsPlayer()
    {
        if (!isAttached || attachedTarget == null || !rb.isKinematic) return;

        Vector3 dirToPlayer = controller.transform.position - transform.position;
        rb.MovePosition(transform.position + dirToPlayer.normalized * pullSpeed * Time.fixedDeltaTime);
    }

    public void YankAndDetach()
    {
        if (!isAttached || attachedTarget == null) return;

        isAttached = false;

        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
        }

        if (isLightObjectAttached && attachedTarget.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            Vector3 yankDir = (controller.transform.position - attachedTarget.transform.position).normalized;
            targetRb.AddForce(yankDir * yankImpulseForce, ForceMode.Impulse);
        }

        ReturnToPlayer();
    }

    private void ReturnToPlayer()
    {
        isReturning = false;
        isAttached = false;
        isLightObjectAttached = false;

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
        if (!isAttached) return;

        if (isLightObjectAttached)
            YankAndDetach();
        else
            ReturnToPlayer();
    }
}