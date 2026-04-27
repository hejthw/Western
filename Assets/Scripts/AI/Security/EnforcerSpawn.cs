using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class EnforcerSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkObject _trainCarPrefab;
    [SerializeField] private NetworkObject[] _npcPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _minNpc = 3;
    [SerializeField] private int _maxNpc = 8;
    [SerializeField] private float _spawnRadius = 3f;

    [Header("Replenish Settings")]
    [SerializeField] private float _checkInterval = 5f;

    private readonly List<NetworkObject> _spawnedNpcs = new();
    

    [Server]
    public void SpawnTrainCar()
    {
        NetworkObject car = Instantiate(_trainCarPrefab, _spawnPoint.position, _spawnPoint.rotation);
        ServerManager.Spawn(car);

        int count = Random.Range(_minNpc, _maxNpc + 1);
        SpawnNpcs(count);

        StartCoroutine(ReplenishCoroutine());

        Debug.Log($"[EnforcerSpawner] Вагон + {count} NPC заспавнены.");
    }

    [Server]
    private void SpawnNpcs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle * _spawnRadius;
            Vector3 pos = _spawnPoint.position + new Vector3(circle.x, 0f, circle.y);

            NetworkObject npcPrefab = _npcPrefabs[Random.Range(0, _npcPrefabs.Length)];
            NetworkObject npc = Instantiate(npcPrefab, pos, Quaternion.identity);
            ServerManager.Spawn(npc);

            _spawnedNpcs.Add(npc);
        }
    }

    private IEnumerator ReplenishCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_checkInterval);

            // Убираем мёртвые/уничтоженные объекты из списка
            _spawnedNpcs.RemoveAll(npc => npc == null);

            int alive = _spawnedNpcs.Count;
            Debug.Log(alive);
            if (alive < _minNpc)
            {
                int toSpawn = _maxNpc - alive;
                Debug.Log($"[EnforcerSpawner] Осталось {alive} NPC, спавним ещё {toSpawn}.");
                SpawnNpcs(toSpawn);
            }
        }
    }
}