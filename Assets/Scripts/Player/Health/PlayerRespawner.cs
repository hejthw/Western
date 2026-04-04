using System;
using FishNet.Object;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerRespawner: NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    private void OnEnable() => PlayerHealthEvents.RespawnEvent += Respawn;
    private void OnDisable() => PlayerHealthEvents.RespawnEvent -= Respawn;
    
    [Server]
    private void Respawn()
    {
        Vector3 spawnPos = GetSpawnPosition();
        transform.position = spawnPos;

        RpcOnRespawned(spawnPos);
    }

    [ObserversRpc]
    private void RpcOnRespawned(Vector3 position)
    {
        transform.position = position;
        Debug.Log($"{gameObject.name} respawned at {position}");
    }
    
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)].position;

        return Vector3.zero;
    }
}