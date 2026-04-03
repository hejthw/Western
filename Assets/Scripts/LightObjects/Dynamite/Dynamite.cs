using FishNet.Object;
using UnityEngine;
using System.Collections;

public class Dynamite : LightObject
{
    [Header("Dynamite Settings")]
    public float explosionDelay = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public LayerMask wallLayer;

    private bool isLit = false;

    // =========================
    // IGNITE
    // =========================

    public void Ignite()
    {
        if (isLit) return;

        if (IsServer)
        {
            StartIgnite();
        }
        else
        {
            ServerIgnite();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerIgnite()
    {
        StartIgnite();
    }

    [Server]
    private void StartIgnite()
    {
        if (isLit) return;

        isLit = true;
        StartCoroutine(ExplodeAfterDelay());
    }

    // =========================
    // EXPLOSION
    // =========================

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    [Server]
    private void Explode()
    {
        // 💥 Находим стены
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider col in hitColliders)
        {
            if (((1 << col.gameObject.layer) & wallLayer) == 0)
                continue;

            RDestructibleWall wall = col.GetComponent<RDestructibleWall>();
            if (wall != null)
            {
                wall.DestroyWallServer(transform.position);
            }

            // 💥 Добавим физику
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        // 💥 Эффекты для всех
        ObserversExplode(transform.position);

        // удаляем объект
        NetworkObject.Despawn();
    }

    [ObserversRpc]
    private void ObserversExplode(Vector3 pos)
    {
        // сюда потом добавишь VFX / звук
        Debug.Log("BOOM at " + pos);
    }
}