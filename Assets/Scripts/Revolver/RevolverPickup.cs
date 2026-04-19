using FishNet.Object;
using FishNet.Managing;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

/// <summary>
/// Пикак револьвера на сцене. Является LightObject
/// </summary>
[RequireComponent(typeof(LightObject))]
public class RevolverPickup : LightObject
{
    public RevolverData revolverData;
    public NetworkObject revolverWeaponPrefab;

    private readonly SyncVar<int> _bullets = new SyncVar<int>();

    public void SetBullets(int count)
    {
        if (IsServer) _bullets.Value = count;
    }

   
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_bullets.Value == 0)
            _bullets.Value = revolverData.bullets;
    }

    public override byte[] SerializeState()
    {
        return System.BitConverter.GetBytes(_bullets.Value);
    }

    public override void DeserializeState(byte[] data)
    {
        if (data != null && data.Length >= 4)
            _bullets.Value = System.BitConverter.ToInt32(data, 0);
    }
    
    [Server]
    protected override bool CanPickup(NetworkObject player)
    {
        if (player == null) return false;
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null) return false;
        return inventory.HasFreeSlot();
    }
    
    [Server]
    protected override bool OnPickup(NetworkObject player)
    {
        if (player == null) return true;

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null) return true;

        int slot = inventory.TryStoreRevolverPickupInSlot(this, player);
        if (slot < 0) return true;

        NetworkObject.Despawn();
        inventory.EquipFromSlot(slot, player);
        return true;
    }

}