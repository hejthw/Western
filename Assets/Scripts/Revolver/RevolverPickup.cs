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

}