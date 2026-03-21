using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Revolver : NetworkBehaviour
{
    public RevolverData revolverData;
    [SerializeField] private Transform muzzle;
    [SerializeField] private NetworkObject revolverPickupPrefab;

    private PlayerInput _input;
    private PlayerController _playerController;
    private float _fireTimer;

    private readonly SyncVar<int> _bullets = new SyncVar<int>(new SyncTypeSettings());

    /// <summary>Вызывается сервером из RevolverPickup для переноса патронов</summary>
    public void SetBullets(int count)
    {
        _bullets.Value = count;
    }
    
    public void AttachToPlayer(PlayerController playerController)
    {
        _playerController = playerController;
        int playerObjId = playerController.GetComponent<NetworkObject>().ObjectId;
        AttachClientRpc(playerObjId);
    }

    /// <summary>
    /// Привязывает револьвер к holdPoint клиента и подписывает на события playerInput.
    /// </summary>
    [ObserversRpc(BufferLast = true)]
    private void AttachClientRpc(int playerNetObjId)
    {
        // Поиск NetworkObject игрока по его ObjectID
        if (!NetworkManager.ClientManager.Objects.Spawned.TryGetValue(
                playerNetObjId, out NetworkObject playerNetObj))
        {
            Debug.LogError($"[Client] Объект с id {playerNetObjId} не найден!");
            return;
        }
        
        // Привязка оружия к holdPoint
        var controller = playerNetObj.GetComponent<PlayerController>();
        transform.SetParent(controller.weaponHoldPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        // Если IsOwner => подписываем на события инпута
        if (!playerNetObj.IsOwner) return;

        _input = playerNetObj.GetComponent<PlayerInput>();
        if (_input != null)
        {
            _input.OnAttackEvent += Shoot;
            _input.OnDropEvent += Drop;
        }
    }

    /// <summary>
    /// Вызывается, когда обьект деспавниться на клиенте.
    /// </summary>
    public override void OnStopClient()
    {
        // Отписка от инпута
        base.OnStopClient();
        if (!IsOwner || _input == null) return;
    
        _input.OnAttackEvent -= Shoot;
        _input.OnDropEvent -= Drop;
    }

    
    private void Update()
    {
        // таймер выстрела
        if (_fireTimer > 0)
            _fireTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Локально проверяет таймер и патроны и отправляет в ServerRpc
    /// </summary>
    private void Shoot()
    {
        if (!IsOwner || _fireTimer > 0f) return;
        if (_bullets.Value <= 0)
        {
            Debug.Log("No bullets");
            return;
        }

        ShootServerRpc(revolverData.damage, muzzle.position, muzzle.forward);
        Debug.Log("remain bullets: " + _bullets.Value);
        _fireTimer = revolverData.timeBeforeShot;
        Debug.DrawRay(muzzle.position, muzzle.forward * 100f, Color.red, 2f);
    }
    
    /// <summary>
    /// Применяет Raycast на сервере
    /// </summary>
    [ServerRpc]
    private void ShootServerRpc(int damage, Vector3 pos, Vector3 dir)
    {
        if (_bullets.Value <= 0) return;

        _bullets.Value--;

        if (Physics.Raycast(pos, dir, out RaycastHit hit)
            && hit.transform.TryGetComponent(out PlayerHealth health))
        {
            health.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Отправка на сервер запроса для спавна пикапа
    /// </summary>
    private void Drop()
    {
        if (!IsOwner) return;
        DropServerRpc(transform.position, transform.rotation);
    }

    /// <summary>
    /// Cпавн пикапа с сохранением кол-ва патрон.
    /// </summary>
    [ServerRpc]
    private void DropServerRpc(Vector3 pos, Quaternion rot)
    {
        if (_playerController != null)
            _playerController.UnequipWeapon();

        NetworkObject pickup = Instantiate(revolverPickupPrefab, pos, rot);
        NetworkManager.ServerManager.Spawn(pickup);

        // передаём текущие патроны в пикап
        pickup.GetComponent<RevolverPickup>().SetBullets(_bullets.Value);

        NetworkObject.Despawn();
    }


}