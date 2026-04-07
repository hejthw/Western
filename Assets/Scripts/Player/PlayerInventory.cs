using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int slotsCount = 3;

    // Структура для хранения данных предмета
    [System.Serializable]
    private class ItemData
    {
        public int prefabId;
        public byte[] state;
    }

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

    // Вызывается из PickupController при сохранении предмета из рук
    [Server]
    public void StoreItemFromHand(int slot, LightObject lightObj)
    {
        if (slot < 0 || slot >= slotsCount) return;
        if (itemPrefabIds[slot] != -1) return; // слот занят

        NetworkObject netObj = lightObj.NetworkObject;
        int prefabId = netObj.PrefabId;
        byte[] state = lightObj.SerializeState();

        // Деспавним предмет (удаляем из мира)
        netObj.Despawn();

        // Сохраняем в слот
        itemPrefabIds[slot] = prefabId;
        itemStates[slot] = state;

        Debug.Log($"[Inventory] Stored item {prefabId} in slot {slot}");
    }

    // Вызывается из PickupController при экипировке предмета из слота
    [Server]
    public void EquipFromSlot(int slot, NetworkObject player)
    {
        if (slot < 0 || slot >= slotsCount) return;
        if (itemPrefabIds[slot] == -1) return;

        int prefabId = itemPrefabIds[slot];
        byte[] state = itemStates[slot];

        // Спавним предмет
        NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
        if (prefab == null)
        {
            Debug.LogError($"[Inventory] Prefab with id {prefabId} not found");
            return;
        }

        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        NetworkObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkManager.ServerManager.Spawn(spawned, Owner);

        // Восстанавливаем состояние
        if (spawned.TryGetComponent<LightObject>(out var lightObj))
        {
            lightObj.DeserializeState(state);
            lightObj.ServerPickup(player);
        }

        // Очищаем слот
        itemPrefabIds[slot] = -1;
        itemStates[slot] = null;

        Debug.Log($"[Inventory] Equipped item {prefabId} from slot {slot}");
    }

    // Для отладки: вывод содержимого инвентаря
    public void DebugPrintInventory()
    {
        string debug = "Inventory: ";
        for (int i = 0; i < slotsCount; i++)
        {
            debug += $"Slot{i}:{(itemPrefabIds[i] == -1 ? "Empty" : itemPrefabIds[i].ToString())} ";
        }
        Debug.Log(debug);
    }
}