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

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    [Server]
    public void SpawnTrainCar()
    {
        // Спавним вагон
        NetworkObject car = Instantiate(_trainCarPrefab, _spawnPoint.position, _spawnPoint.rotation);
        ServerManager.Spawn(car);

        // Спавним NPC вокруг вагона
        int count = Random.Range(_minNpc, _maxNpc + 1);

        for (int i = 0; i < count; i++)
        {
            Vector2 circle = Random.insideUnitCircle * _spawnRadius;
            Vector3 pos = _spawnPoint.position + new Vector3(circle.x, 0f, circle.y);

            NetworkObject npcPrefab = _npcPrefabs[Random.Range(0, _npcPrefabs.Length)];
            NetworkObject npc = Instantiate(npcPrefab, pos, Quaternion.identity);
            ServerManager.Spawn(npc);
        }

        Debug.Log($"[TrainCarSpawner] Вагон + {count} NPC заспавнены.");
    }
}