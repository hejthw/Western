using FishNet.Object;
using FishNet.Managing;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

/// <summary>
/// Пикак револьвера на сцене. Является LightObject
/// </summary>
[RequireComponent(typeof(LightObject))]
public class RevolverPickup : NetworkBehaviour, IPickupable
{
    [SerializeField] private NetworkObject revolverWeaponPrefab;
    [SerializeField] public RevolverData revolverData;
    
    private readonly SyncVar<int> _bullets = new SyncVar<int>(new SyncTypeSettings());
    
    /// <summary> Вызывается при спавне на сцене. Устанавливается кол-во патрон</summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        _bullets.Value = revolverData.bullets;
    }
    
    /// <summary> Вызывается сервером при дропе оружия для передачи патронов</summary>
    public void SetBullets(int count)
    {
        _bullets.Value = count;
        
        // если нет патрон => обьект удалиться через n секунд
        if (count == 0)
            StartCoroutine(DespawnAfterDelay(revolverData.timeBeforeDespawn));
    }

    /// <summary>
    /// Вызывывается из PickupController
    /// </summary>
    /// <param name="player"></param>
    public void Interact(GameObject player)
    {
        if (!IsServer) return;

        var playerController = player.GetComponent<PlayerController>();
        if (playerController == null) 
        {
            Debug.LogError("PlayerController не найден!"); 
            return; 
        }

        var playerNetObj = player.GetComponent<NetworkObject>();

        // Спавн префаб оружия у игрока
        NetworkObject weaponInstance = Instantiate(
            revolverWeaponPrefab,
            playerController.weaponHoldPoint.position,
            playerController.weaponHoldPoint.rotation
        );

        // Передача IsOwner клиенту, который подобрал оружие
        NetworkManager.ServerManager.Spawn(weaponInstance, playerNetObj.Owner);

        Revolver revolver = weaponInstance.GetComponent<Revolver>();
        revolver.SetBullets(_bullets.Value);
        revolver.AttachToPlayer(playerController);
        playerController.EquipWeapon(revolver);

        NetworkObject.Despawn();
    }
    
    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsSpawned)
            NetworkObject.Despawn();
    }
}