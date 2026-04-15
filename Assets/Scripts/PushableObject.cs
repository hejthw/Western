using UnityEngine;
using FishNet.Object;

public class PushableObject : NetworkBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent<PlayerPhysics>(out var player))
        {
            Vector3 pushDirection = collision.contacts[0].normal;
            float pushForce = player.GetComponent<Rigidbody>().linearVelocity.magnitude * 3f;

            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
        }
    }
}