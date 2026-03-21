using FishNet.Object;
using UnityEngine;

public class Revolver : NetworkBehaviour
{
    public RevolverData revolverData;
    [SerializeField] private Transform muzzle;
    [SerializeField] private NetworkObject revolverPickupPrefab;

    private PlayerInput _input;
    private PlayerController _playerController;
    private float _fireTimer;
    
    // Revolver.cs
    public void AttachToPlayer(PlayerController playerController)
    {
        Debug.Log("[Server] AttachToPlayer вызван");
        _playerController = playerController;
        int playerObjId = playerController.GetComponent<NetworkObject>().ObjectId;
        Debug.Log($"[Server] Отправляем AttachClientRpc с playerObjId: {playerObjId}");
        AttachClientRpc(playerObjId);
    }

    [ObserversRpc(BufferLast = true)]
    private void AttachClientRpc(int playerNetObjId)
    {
        if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(
                playerNetObjId, out NetworkObject playerNetObj))
        {
            Debug.LogError($"[Client] Объект с id {playerNetObjId} не найден!");
            return;
        }

        var controller = playerNetObj.GetComponent<PlayerController>();
        transform.SetParent(controller.weaponHoldPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Проверяем через владельца NetworkObject игрока, а не IsOwner оружия
        bool isLocalPlayer = playerNetObj.IsOwner;
        Debug.Log($"[Client] isLocalPlayer: {isLocalPlayer}");
        if (!isLocalPlayer) return;

        _input = playerNetObj.GetComponent<PlayerInput>();
        Debug.Log($"[Client] PlayerInput найден: {_input != null}");

        if (_input != null)
        {
            _input.OnAttackEvent += Shoot;
            _input.OnDropEvent += Drop;
            Debug.Log("[Client] Подписка выполнена");
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!IsOwner || _input == null) return;

        _input.OnAttackEvent -= Shoot;
        _input.OnDropEvent -= Drop;
    }

    private void Update()
    {
        if (_fireTimer > 0)
            _fireTimer -= Time.deltaTime;
    }

    // Revolver.cs
    private void Shoot()
    {
        Debug.Log($"[Revolver] Shoot вызван. IsOwner: {IsOwner}, _fireTimer: {_fireTimer}");
        if (!IsOwner || _fireTimer > 0f) return;
        Debug.Log("[Revolver] Стреляем!");
        ShootServerRpc(revolverData.damage, muzzle.position, muzzle.forward);
        _fireTimer = revolverData.timeBeforeShot;
        Debug.DrawRay(muzzle.position, muzzle.forward * 100f, Color.red, 2f);
    }

    private void Drop()
    {
        if (!IsOwner) return;
        DropServerRpc(transform.position, transform.rotation);
    }

    [ServerRpc]
    private void DropServerRpc(Vector3 pos, Quaternion rot)
    {
        if (_playerController != null)
            _playerController.UnequipWeapon();

        NetworkObject pickup = Instantiate(revolverPickupPrefab, pos, rot);
        NetworkManager.ServerManager.Spawn(pickup);
        NetworkObject.Despawn();
    }

    [ServerRpc]
    private void ShootServerRpc(int damage, Vector3 pos, Vector3 dir)
    {
        if (Physics.Raycast(pos, dir, out RaycastHit hit)
            && hit.transform.TryGetComponent(out PlayerHealth health))
        {
            health.TakeDamage(damage);
        }
    }
}