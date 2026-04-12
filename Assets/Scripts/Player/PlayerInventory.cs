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

    public bool IsSlotEmpty(int slot) => itemPrefabIds[slot] == -1;
    public int GetItemPrefabId(int slot) => itemPrefabIds[slot];
    public byte[] GetItemState(int slot) => itemStates[slot];

   
    [Server]
    public void StoreItemFromHand(int slot, LightObject lightObj)
    {
        if (slot < 0 || slot >= slotsCount) return;
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
        if (slot < 0 || slot >= slotsCount) return;
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
                playerController.EquipWeapon(revolver);
            }
        }
        else
        {
            NetworkObject spawned = Instantiate(prefab, player.transform.position, Quaternion.identity);
            NetworkManager.ServerManager.Spawn(spawned, player.Owner);

            LightObject lightObj = spawned.GetComponent<LightObject>();
            if (lightObj != null)
            {
                lightObj.DeserializeState(state);
                lightObj.EquipToPlayer(player);
            }
        }

       
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