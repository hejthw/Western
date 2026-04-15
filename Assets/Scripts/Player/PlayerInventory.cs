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
    
    [Server]
    public bool TryStoreRevolverAndEquip(RevolverPickup pickup, NetworkObject player)
    {
        if (pickup == null || player == null) return false;
        
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null) return false;
        if (playerController.IsArmed) return false;
        
        int slot = GetFirstEmptySlot();
        if (slot == -1) return false;
        
        if (pickup.revolverWeaponPrefab == null) return false;
        
        itemPrefabIds[slot] = pickup.NetworkObject.PrefabId;
        itemStates[slot] = pickup.SerializeState();
        OnSlotChanged?.Invoke(slot, true);
        
        int bullets = pickup.revolverData != null ? pickup.revolverData.bullets : 0;
        if (itemStates[slot] != null && itemStates[slot].Length >= 4)
            bullets = System.BitConverter.ToInt32(itemStates[slot], 0);
        
        NetworkObject weaponInstance = Instantiate(
            pickup.revolverWeaponPrefab,
            playerController.weaponHoldPoint.position,
            playerController.weaponHoldPoint.rotation
        );
        NetworkManager.ServerManager.Spawn(weaponInstance, player.Owner);
        
        Revolver revolver = weaponInstance.GetComponent<Revolver>();
        if (revolver == null)
        {
            weaponInstance.Despawn();
            ClearSlot(slot);
            return false;
        }
        
        revolver.SetBullets(bullets);
        revolver.AttachToPlayer(playerController, bullets);
        revolver.BindInventorySlot(this, slot, pickup.NetworkObject.PrefabId);
        playerController.EquipWeapon(revolver);
        return true;
    }
    
    [Server]
    public void UpdateSlotState(int slot, byte[] state)
    {
        if (!IsValidSlot(slot)) return;
        if (itemPrefabIds[slot] == -1) return;
        itemStates[slot] = state;
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
}