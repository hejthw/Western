using UnityEngine;
using FishNet.Object;

public class PushableObject : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float pushForceMultiplier = 3f;
    [SerializeField] private float minPushForce = 1f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent<PlayerPhysics>(out var player))
        {
            Vector3 pushDirection = collision.contacts[0].normal;
            float playerSpeed = player.GetComponent<Rigidbody>().linearVelocity.magnitude;
            float pushForce = playerSpeed * pushForceMultiplier;
            if (pushForce < minPushForce) pushForce = minPushForce;

            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        }
    }
}