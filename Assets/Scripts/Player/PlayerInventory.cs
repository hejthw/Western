using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
using FishNet.Connection;
using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int slotsCount = 3;
    public event System.Action<int, bool> OnSlotChanged;
    public int SlotsCount => slotsCount;

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
        itemPrefabIds.OnChange += ItemPrefabIds_OnChange;
    }
    private void ItemPrefabIds_OnChange(SyncListOperation op, int index, int oldItem, int newItem, bool asServer)
    {
        if (asServer) return;

        bool isOccupied = newItem != -1;
        OnSlotChanged?.Invoke(index, isOccupied);
    }

    public bool IsSlotEmpty(int slot) => IsValidSlot(slot) && itemPrefabIds[slot] == -1;
    public int GetItemPrefabId(int slot) => IsValidSlot(slot) ? itemPrefabIds[slot] : -1;
    public byte[] GetItemState(int slot) => IsValidSlot(slot) ? itemStates[slot] : null;
    
    public int GetFirstEmptySlot()
    {
        for (int i = 0; i < slotsCount; i++)
        {
            if (itemPrefabIds[i] == -1)
                return i;
        }
        
        return -1;
    }
    
    public bool HasFreeSlot()
    {
        return GetFirstEmptySlot() != -1;
    }
    
    private bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < slotsCount;
    }

   
    [Server]
    public void StoreItemFromHand(int slot, LightObject lightObj)
    {
        if (!IsValidSlot(slot)) return;
        if (itemPrefabIds[slot] != -1) return; 
        NetworkObject netObj = lightObj.NetworkObject;
        int prefabId = netObj.PrefabId;
        byte[] state = lightObj.SerializeState();
        lightObj.HideVisualForObservers();
        netObj.Despawn();
        itemPrefabIds[slot] = prefabId;
        itemStates[slot] = state;
        OnSlotChanged?.Invoke(slot, true);
        Debug.Log($"[Inventory] Stored item {prefabId} in slot {slot}");
    }

    [Server]
    public void EquipFromSlot(int slot, NetworkObject player)
    {
        if (!IsValidSlot(slot)) return;
        if (itemPrefabIds[slot] == -1) return;

        int prefabId = itemPrefabIds[slot];
        byte[] state = itemStates[slot];

        NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
        if (prefab == null) return;
        RevolverPickup revolverPickupPrefab = prefab.GetComponent<RevolverPickup>();
        if (revolverPickupPrefab != null && revolverPickupPrefab.revolverWeaponPrefab != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                if (playerController.IsArmed) return;

                int bullets = revolverPickupPrefab.revolverData.bullets;
                if (state != null && state.Length >= 4)
                    bullets = System.BitConverter.ToInt32(state, 0);

                NetworkObject weaponInstance = Instantiate(
                    revolverPickupPrefab.revolverWeaponPrefab,
                    playerController.weaponHoldPoint.position,
                    playerController.weaponHoldPoint.rotation
                );
                NetworkManager.ServerManager.Spawn(weaponInstance, player.Owner);

                Revolver revolver = weaponInstance.GetComponent<Revolver>();
                if (revolver == null)
                {
                    weaponInstance.Despawn();
                    ClearSlot(slot);
                    return;
                }

                revolver.SetBullets(bullets);
                revolver.AttachToPlayer(playerController, bullets);
                revolver.BindInventorySlot(this, slot, prefabId);
                playerController.EquipWeapon(revolver);
            }
            return;
        }
        
        NetworkObject spawned = Instantiate(prefab, player.transform.position, Quaternion.identity);
        NetworkManager.ServerManager.Spawn(spawned, player.Owner);

        LightObject lightObj = spawned.GetComponent<LightObject>();
        if (lightObj != null)
        {
            lightObj.DeserializeState(state);
            lightObj.EquipToPlayer(player);
        }
        
        ClearSlot(slot);
    }
    
    /// <summary>
    /// Только запись пикапа в свободный слот (prefab id + состояние). Без спавна оружия.
    /// </summary>
    [Server]
    public int TryStoreRevolverPickupInSlot(RevolverPickup pickup, NetworkObject player)
    {
        if (pickup == null || player == null) return -1;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null) return -1;
        if (playerController.IsArmed) return -1;

        int slot = GetFirstEmptySlot();
        if (slot == -1) return -1;
        if (pickup.revolverWeaponPrefab == null) return -1;

        itemPrefabIds[slot] = pickup.NetworkObject.PrefabId;
        itemStates[slot] = pickup.SerializeState();
        OnSlotChanged?.Invoke(slot, true);
        return slot;
    }

    /// <summary>
    /// Подбор с поля: положить в инвентарь и сразу экипировать через <see cref="EquipFromSlot"/> (один код пути).
    /// </summary>
    [Server]
    public bool TryStoreRevolverAndEquip(RevolverPickup pickup, NetworkObject player)
    {
        int slot = TryStoreRevolverPickupInSlot(pickup, player);
        if (slot < 0) return false;
        EquipFromSlot(slot, player);
        return true;
    }
    
    [Server]
    public void UpdateSlotState(int slot, byte[] state)
    {
        if (!IsValidSlot(slot)) return;
        if (itemPrefabIds[slot] == -1) return;
        itemStates[slot] = state;
    }

    /// <summary>
    /// Переносит запись револьвера в инвентаре на другой слот без деспавна оружия (игрок держит револьвер в руках).
    /// Если целевой слот занят — обменивает содержимое слотов.
    /// </summary>
    [Server]
    public void MoveBoundRevolverToSlot(Revolver revolver, int toSlot)
    {
        if (revolver == null) return;
        int fromSlot = revolver.GetBoundSlot();
        if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot)) return;
        if (fromSlot == toSlot) return;
        if (itemPrefabIds[fromSlot] == -1) return;

        int boundPickupId = revolver.GetBoundPickupPrefabId();
        if (boundPickupId >= 0 && itemPrefabIds[fromSlot] != boundPickupId)
            return;

        revolver.SaveBulletsToInventorySlot();

        int pid = itemPrefabIds[fromSlot];
        byte[] st = itemStates[fromSlot];

        if (itemPrefabIds[toSlot] == -1)
        {
            itemPrefabIds[toSlot] = pid;
            itemStates[toSlot] = st;
            itemPrefabIds[fromSlot] = -1;
            itemStates[fromSlot] = null;
            OnSlotChanged?.Invoke(toSlot, true);
            OnSlotChanged?.Invoke(fromSlot, false);
        }
        else
        {
            int pid2 = itemPrefabIds[toSlot];
            byte[] st2 = itemStates[toSlot];
            itemPrefabIds[fromSlot] = pid2;
            itemStates[fromSlot] = st2;
            itemPrefabIds[toSlot] = pid;
            itemStates[toSlot] = st;
            OnSlotChanged?.Invoke(fromSlot, pid2 != -1);
            OnSlotChanged?.Invoke(toSlot, true);
        }

        revolver.BindInventorySlot(this, toSlot, pid);
    }
    
    [Server]
    public void ClearSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;
        if (itemPrefabIds[slot] == -1) return;
        
        itemPrefabIds[slot] = -1;
        itemStates[slot] = null;
        OnSlotChanged?.Invoke(slot, false);
    }

    public void DebugPrintInventory()
    {
        string debug = "Inventory: ";
        for (int i = 0; i < slotsCount; i++)
        {
            debug += $"Slot{i}:{(itemPrefabIds[i] == -1 ? "Empty" : itemPrefabIds[i].ToString())} ";
        }
        Debug.Log(debug);
    }
    [Server]
    public void UnequipRevolver(NetworkObject player)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) return;
        Revolver revolver = pc.GetCurrentWeapon();
        if (revolver == null) return;
        revolver.UnequipToInventory();
    }
    [Server]
    public void DropAllItems(Vector3 dropPosition)
    {
        for (int i = 0; i < slotsCount; i++)
        {
            if (itemPrefabIds[i] == -1) continue;

            int prefabId = itemPrefabIds[i];
            byte[] state = itemStates[i];

            NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
            if (prefab == null) continue;

            NetworkObject dropped = Instantiate(prefab, dropPosition, Quaternion.identity);
            NetworkManager.ServerManager.Spawn(dropped);

           
            LightObject lightObj = dropped.GetComponent<LightObject>();
            if (lightObj != null)
            {
                lightObj.DeserializeState(state);
                lightObj.SetVisibleForObservers(true);
            }

            ClearSlot(i);
        }
    }
}