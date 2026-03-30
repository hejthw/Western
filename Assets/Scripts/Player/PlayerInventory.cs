using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
using FishNet.Connection;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int slotsCount = 3;
    private readonly SyncList<int> itemPrefabIds = new SyncList<int>();
    private readonly SyncList<byte[]> itemStates = new SyncList<byte[]>();

    private void Awake()
    {
        for (int i = 0; i < slotsCount; i++)
        {
            itemPrefabIds.Add(-1);
            itemStates.Add(null);
        }
    }

    public bool IsSlotEmpty(int slot) => itemPrefabIds[slot] == -1;

    public int GetItemPrefabId(int slot) => itemPrefabIds[slot];
    public byte[] GetItemState(int slot) => itemStates[slot];

    [ServerRpc(RequireOwnership = false)]
    public void ServerStoreItem(int slot, int prefabId, byte[] state)
    {
        if (slot < 0 || slot >= slotsCount) return;
        if (itemPrefabIds[slot] != -1) return;

        itemPrefabIds[slot] = prefabId;
        itemStates[slot] = state;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRemoveItem(int slot)
    {
        if (slot < 0 || slot >= slotsCount) return;
        if (itemPrefabIds[slot] == -1) return;

        int prefabId = itemPrefabIds[slot];
        byte[] state = itemStates[slot];
        itemPrefabIds[slot] = -1;
        itemStates[slot] = null;

        NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
        if (prefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        NetworkObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkManager.ServerManager.Spawn(spawned, Owner);

   
        if (spawned.TryGetComponent(out ISavableItem savable))
            savable.LoadState(state);

        TargetEquipItem(Owner, spawned);
    }

    [TargetRpc]
    private void TargetEquipItem(NetworkConnection target, NetworkObject item)
    {
        var pickup = GetComponent<PickupController>();
        if (pickup != null)
            pickup.PickupItem(item.gameObject);
    }
}