using UnityEngine;
using FishNet.Object;

public class LootSpawner : NetworkBehaviour
{
    [SerializeField] private LootDatabase database;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int totalLootCount = 20;

    private int totalValue;

    public override void OnStartServer()
    {
        base.OnStartServer();

        SpawnLoot();
    }

    [Server]
    private void SpawnLoot()
    {
        if (database == null || database.items == null || database.items.Length == 0)
        {
            Debug.LogError("LootDatabase пустой");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Нет точек спавна");
            return;
        }

        totalValue = 0;

        for (int i = 0; i < totalLootCount; i++)
        {
            var data = database.items[Random.Range(0, database.items.Length)];
            if (data == null) continue;

            Transform point = spawnPoints[i % spawnPoints.Length];

            var obj = Instantiate(data.prefab, point.position, Quaternion.identity);
            var netObj = obj.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("Нет NetworkObject на префабе");
                Destroy(obj);
                continue;
            }

            var treasure = obj.GetComponent<Treasure>();
            if (treasure != null)
                treasure.SetData(data);

            Spawn(netObj);

            totalValue += data.value;
        }

        Debug.Log("TOTAL VALUE: " + totalValue);

        if (HeistManager.Instance != null)
            HeistManager.Instance.SetTotalValue(totalValue);
        else
            Debug.LogError("HeistManager.Instance NULL");
    }
}