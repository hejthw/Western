using FishNet.Object;
using FishNet.Managing;
using UnityEngine;

// Этот префаб лежит в мире — просто физический объект
[RequireComponent(typeof(LightObject))]
public class RevolverPickup : NetworkBehaviour, IPickupable
{
    [SerializeField] private NetworkObject revolverWeaponPrefab; // ссылка на второй префаб

    // RevolverPickup.cs
    public void Interact(GameObject player)
    {
        if (!IsServer) return;

        var playerController = player.GetComponent<PlayerController>();
        if (playerController == null) { Debug.LogError("PlayerController не найден!"); return; }

        var playerNetObj = player.GetComponent<NetworkObject>();

        NetworkObject weaponInstance = Instantiate(
            revolverWeaponPrefab,
            playerController.weaponHoldPoint.position,
            playerController.weaponHoldPoint.rotation
        );

        // Передаём владение конкретному клиенту-владельцу игрока
        NetworkManager.ServerManager.Spawn(weaponInstance, playerNetObj.Owner);
        Debug.Log($"[Server] Оружие заспавнено, owner: {playerNetObj.Owner.ClientId}");

        Revolver revolver = weaponInstance.GetComponent<Revolver>();
        revolver.AttachToPlayer(playerController);
        playerController.EquipWeapon(revolver);

        NetworkObject.Despawn();
    }
}