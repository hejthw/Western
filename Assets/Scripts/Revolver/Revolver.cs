using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Revolver: NetworkBehaviour
{
    public RevolverData revolverData;
    [SerializeField] private Transform muzzle;
    [SerializeField] public NetworkObject revolverPickupPrefab;
    
    [SerializeField] private NetworkObject bulletPrefab;

    private PlayerInput _input;
    private PlayerController _playerController;
    private float _fireTimer;
    private RevolverRecoil _recoil;
    private LayerMask _hitboxMask;
    private PlayerInventory _boundInventory;
    private int _boundSlot = -1;
    private int _boundPickupPrefabId = -1;

    private readonly SyncVar<int> _bullets = new SyncVar<int>();
    
    public int GetBullets() => _bullets.Value;

    private void Awake()
    {
        _hitboxMask = LayerMask.GetMask("Hitbox","Breakable");
    }

    private void Update()
    {
        if (_fireTimer > 0f)
            _fireTimer -= Time.deltaTime;
    }

    public void SetBullets(int count) => _bullets.Value = count;

    public void AttachToPlayer(PlayerController playerController, int bullets)
    {
        _playerController = playerController;
        int playerObjId = playerController.GetComponent<NetworkObject>().ObjectId;
        AttachClientRpc(playerObjId, bullets);
    }

    [ObserversRpc(BufferLast = true)]
    private void AttachClientRpc(int playerNetObjId, int bullets)
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

        _bullets.Value = bullets; 

        if (!playerNetObj.IsOwner) return;

        var pickup = playerNetObj.GetComponent<PickupController>();
        if (pickup != null) pickup.SetHeldWeapon(gameObject);

        _recoil = GetComponent<RevolverRecoil>();
        _recoil?.Init(revolverData, controller);

        _input = playerNetObj.GetComponent<PlayerInput>();
        if (_input != null)
        {
            _input.OnAttackEvent += Shoot;
            _input.OnDropEvent += Drop;
        }
    }


    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!IsOwner || _input == null) return;

        _recoil?.ResetImmediate();
        _input.OnAttackEvent -= Shoot;
        _input.OnDropEvent -= Drop;
    }
    
    private void Shoot()
    {
        if (!IsOwner || _fireTimer > 0f) return;
        if (_bullets.Value <= 0) { Debug.Log("No bullets"); return; }

        _recoil?.AddRecoil();

        Vector3 origin = muzzle.position;
        Vector3 direction = muzzle.forward;

        ShootServerRpc(origin, direction);
        SoundBus.Play(SoundID.Shoot);
        _fireTimer = revolverData.timeBeforeShot;
        
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        if (_bullets.Value <= 0) return;
        _bullets.Value--;
        SaveBulletsToInventorySlot();

        SpawnBullet(pos, dir);
    }
    
    private void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        if (bulletPrefab == null) return;

        NetworkObject bulletObj = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        NetworkManager.ServerManager.Spawn(bulletObj);
        bulletObj.GetComponent<Bullet>().Init(revolverData.damage, _hitboxMask);
    }

    private void Drop()
    {
        if (!IsOwner) return;
        DropServerRpc(transform.position, transform.rotation);
    }

    [ServerRpc]
    private void DropServerRpc(Vector3 pos, Quaternion rot)
    {
        _playerController?.UnequipWeapon();

        // GetPooledInstantiate
        NetworkObject pickup = Instantiate(revolverPickupPrefab, pos, rot);
        NetworkManager.ServerManager.Spawn(pickup);
        pickup.GetComponent<RevolverPickup>().SetBullets(_bullets.Value);
        
        if (_boundInventory != null && _boundSlot >= 0)
            _boundInventory.ClearSlot(_boundSlot);

        NetworkObject.Despawn();
    }
    
    [Server]
    public void BindInventorySlot(PlayerInventory inventory, int slot, int pickupPrefabId)
    {
        _boundInventory = inventory;
        _boundSlot = slot;
        _boundPickupPrefabId = pickupPrefabId;
        SaveBulletsToInventorySlot();
    }
    
    [Server]
    private void SaveBulletsToInventorySlot()
    {
        if (_boundInventory == null || _boundSlot < 0) return;
        
        if (_boundInventory.GetItemPrefabId(_boundSlot) != _boundPickupPrefabId)
            return;
        
        _boundInventory.UpdateSlotState(_boundSlot, System.BitConverter.GetBytes(_bullets.Value));
    }
}