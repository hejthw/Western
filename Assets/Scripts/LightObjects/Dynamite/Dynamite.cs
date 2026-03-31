using FishNet.Object;
using UnityEngine;
using System.Collections;

public class Dynamite : LightObject
{
    [Header("Dynamite Settings")]
    public float explosionDelay = 5f;
    public float explosionRadius = 5f;
    public float explosionForce = 500f;   
    public LayerMask wallLayer;           

    private bool isLit = false;

    public void Ignite()
    {
        if (isLit) return;
        isLit = true;
        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    private void Explode()
    {
        if (IsServer)
        {
            
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, wallLayer);
            foreach (Collider col in hitColliders)
            {
                RDestructibleWall wall = col.GetComponent<RDestructibleWall>();
                if (wall != null)
                {
                    wall.DestroyWallServer(transform.position);
                }
            }
       
            NetworkObject.Despawn();
        }
        else
        {
            ServerExplode();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerExplode()
    {
        Explode();
    }
}