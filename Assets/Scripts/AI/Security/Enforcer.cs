using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Enforcer: NetworkNPC
{
    public NPCAttackData AttackData;
    public Transform RevolverMuzzle;
    public RevolverRecoilAI RevolverRecoilAI;
    public NetworkObject bulletPrefab;
    public LayerMask layerMask;
    
    
    [Server]
    public void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        SpawnBullet(pos, dir);
    }
    
    [Server]
    public void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        if (bulletPrefab == null) return;

        NetworkObject bulletObj = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        NetworkManager.ServerManager.Spawn(bulletObj);
        bulletObj.GetComponent<Bullet>().Init(AttackData.Damage,layerMask);
    }
}