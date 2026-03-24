using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
using FishNet.Connection;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int slotsCount = 3;
    private readonly SyncList<int> itemPrefabIds = new SyncList<int>();

    private void Awake()
    {
        for (int i = 0; i < slotsCount; i++)
            itemPrefabIds.Add(-1);
    }

    public bool IsSlotEmpty(int slot)
    {
        if (slot < 0 || slot >= slotsCount) return true;
        return itemPrefabIds[slot] == -1;
    }

    public int GetItemPrefabId(int slot)
    {
        if (slot < 0 || slot >= slotsCount) return -1;
        return itemPrefabIds[slot];
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerStoreItem(int slot, int prefabId)
    {
        if (slot < 0 || slot >= slotsCount) return;
        if (itemPrefabIds[slot] != -1) return;
        itemPrefabIds[slot] = prefabId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRemoveItem(int slot)
    {
        if (slot < 0 || slot >= slotsCount) return;
        if (itemPrefabIds[slot] == -1) return;

        int prefabId = itemPrefabIds[slot];
        itemPrefabIds[slot] = -1;

        NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
        if (prefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        NetworkObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkManager.ServerManager.Spawn(spawned, Owner);
        TargetEquipItem(Owner, spawned);
    }

    [TargetRpc]
    private void TargetEquipItem(NetworkConnection target, NetworkObject item)
    {
        var pickup = GetComponent<PickupController>();
        if (pickup != null)
        {
            pickup.PickupItem(item.gameObject);
        }
    }
}