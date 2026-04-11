using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

public class Dynamite : LightObject, IUsable
{
    [Header("Dynamite Settings")]
    public float explosionDelay = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public LayerMask wallLayer;

    private readonly SyncVar<bool> _isLit = new SyncVar<bool>();
    public bool IsLit => _isLit.Value;

    public void Use()
    {
        Debug.Log("[Dynamite] Use() called");
        Ignite();
    }
    public void Ignite()
    {
        Debug.Log($"[Dynamite] Ignite() called. isLit={IsLit}, parent={transform.parent?.name}");
        if (IsLit) return;

        // Проверка на клиенте: предмет в руках
        bool isHeld = transform.parent != null && transform.parent.CompareTag("HoldPoint");
        if (!isHeld)
        {
            Debug.Log("[Dynamite] Ignite aborted: not held");
            return;
        }

        if (IsServer)
            StartIgnite();
        else
            ServerIgnite();
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerIgnite()
    {
        Debug.Log("[Dynamite] ServerIgnite RPC received on server");
        StartIgnite();
    }

    [Server]
    private void StartIgnite()
    {
        Debug.Log("[Dynamite] StartIgnite on server - igniting without state check");
        if (IsLit) return;

        _isLit.Value = true;
        Debug.Log("[Dynamite] _isLit set to true, starting coroutine");
        ObserversIgniteEffect();
        StartCoroutine(ExplodeAfterDelay());
    }

    [ObserversRpc]
    private void ObserversIgniteEffect()
    {
        Debug.Log("Dynamite lit!");
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    [Server]
    private void Explode()
    {
        if (holder.Value != null)
            ForceDrop();
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            PlayerHealth health = hit.GetComponent<PlayerHealth>();
            if (health != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                int damage = Mathf.RoundToInt(100 * (1 - distance / explosionRadius));
                health.TakeDamage(damage);
            }

            if (((1 << hit.gameObject.layer) & wallLayer) != 0)
            {
                RDestructibleWall wall = hit.GetComponent<RDestructibleWall>();
                if (wall != null)
                    wall.DestroyWallServer(transform.position);
            }

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null && rb.gameObject != gameObject)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        ObserversExplode(transform.position);
        NetworkObject.Despawn();
    }

    [ObserversRpc]
    private void ObserversExplode(Vector3 pos)
    {
        Debug.Log("BOOM at " + pos);
    }

    public override byte[] SerializeState()
    {
        byte[] data = new byte[1];
        data[0] = (byte)(IsLit ? 1 : 0);
        return data;
    }

    public override void DeserializeState(byte[] data)
    {
        if (data != null && data.Length > 0)
        {
            _isLit.Value = data[0] == 1;
        }
    }
}