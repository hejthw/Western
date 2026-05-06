using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using FishNet.Connection;

public class Revolver: NetworkBehaviour, IWeapon
{
    public RevolverData revolverData;
    [SerializeField] private Transform muzzle;
    [SerializeField] public NetworkObject revolverPickupPrefab;
    [SerializeField] private RevolverCylinder cylinder;
    [SerializeField] private NetworkObject bulletPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;

    private PlayerInput _input;
    private PlayerController _playerController;
    private float _fireTimer;
    private RevolverRecoil _recoil;
    private LayerMask _hitboxMask;
    private PlayerInventory _boundInventory;
    private int _boundSlot = -1;
    private int _boundPickupPrefabId = -1;

    private readonly SyncVar<int> _bullets = new SyncVar<int>();

    /// <summary>
    /// Патроны для проверки на клиенте: в AttachClientRpc нельзя надёжно записать SyncVar с клиента
    /// (CanNetworkSetValues), из‑за чего _bullets остаётся 0 до репликации — стрельба блокируется.
    /// </summary>
    private int _clientAmmo;

    public int GetBoundSlot() => _boundSlot;

    public int GetBoundPickupPrefabId() => _boundPickupPrefabId;

    /// <summary>На клиентах отражает _clientAmmo (RPC + репликация), на выделенном сервере — SyncVar.</summary>
    public int GetBullets()
    {
        if (IsServerInitialized && !IsClientInitialized)
            return _bullets.Value;
        return _clientAmmo;
    }

    private void Awake()
    {
        _hitboxMask = LayerMask.GetMask("Hitbox","Breakable");
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _bullets.OnChange += OnBulletsChanged;
    }

    public override void OnStopNetwork()
    {
        _bullets.OnChange -= OnBulletsChanged;
        base.OnStopNetwork();
    }

    private void OnBulletsChanged(int prev, int next, bool asServer)
    {
        if (asServer) return;
        _clientAmmo = next;
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
        _playerController = controller;
        transform.SetParent(controller.weaponHoldPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Не пишем в SyncVar здесь с клиента — см. _clientAmmo. Сервер уже выставил патроны в SetBullets.
        _clientAmmo = bullets;

        if (!playerNetObj.IsOwner) return;

        // Иначе на клиенте не выставлены _currentWeapon / IsArmed — валидация и UI расходятся с сервером.
        if (controller != null)
            controller.EquipWeapon(this);

        var pickup = playerNetObj.GetComponent<PickupController>();
        if (pickup != null) pickup.SetHeldWeapon(gameObject);

        _recoil = GetComponent<RevolverRecoil>();
        _recoil?.Init(revolverData, controller);

        _input = playerNetObj.GetComponent<PlayerInput>();
        if (_input != null)
        {
            _input.OnAttackEvent += Shoot;
            _input.OnDropRevolver += Drop;
        }
    }


    public override void OnStopClient()
    {
        base.OnStopClient();
        if (_playerController == null || !_playerController.IsOwner || _input == null) return;

        _recoil?.ResetImmediate();
        _input.OnAttackEvent -= Shoot;
        _input.OnDropRevolver -= Drop;
    }
    
    private void Shoot()
    {
        // Подписка на атаку идёт по IsOwner игрока; владение NetworkObject револьвера на клиенте может
        // при первом спавне (подбор с пола) ещё не совпасть — стрельбу разрешаем по владельцу игрока.
        if (_playerController == null || !_playerController.IsOwner || _fireTimer > 0f) return;
        cylinder?.Spin();
        SoundBus.Play(SoundID.RevolverCylinder);

        if (_clientAmmo <= 0)
        {
            SoundBus.Play(SoundID.ShootNoBullets);
            return;
        }

        _recoil?.AddRecoil();
        
        Vector3 origin = muzzle.position;
        Vector3 direction = muzzle.forward;

        _playerController.RequestRevolverShoot(NetworkObject, origin, direction);
        PlayerEvents.RaiseSuspicion(SuspicionType.RevolverShoot);

        SoundBus.Play(_clientAmmo == 1 ? SoundID.ShootLastBullet : SoundID.Shoot);

        _fireTimer = revolverData.timeBeforeShot;
    }

    [Server]
    public void ServerApplyShot(Vector3 pos, Vector3 dir)
    {
        if (_bullets.Value <= 0) return;
        _bullets.Value--;
        SaveBulletsToInventorySlot();

        SpawnBullet(pos, dir);
        PlayMuzzleFlashRpc(pos, Quaternion.LookRotation(dir));
    }
    
    private void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        if (bulletPrefab == null) return;

        NetworkObject bulletObj = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        NetworkManager.ServerManager.Spawn(bulletObj);
        bulletObj.GetComponent<Bullet>().Init(revolverData.damage, _hitboxMask);
    }

    public void Drop()
    {
        if (_playerController == null || !_playerController.IsOwner) return;
        DropServerRpc(transform.position, transform.rotation);
    }

    [ServerRpc]
    private void DropServerRpc(Vector3 pos, Quaternion rot)
    {
        Debug.Log("[Revolver] DropServerRpc");
        _playerController?.UnequipWeapon();
        NetworkObject pickup = Instantiate(revolverPickupPrefab, pos, rot);
        NetworkManager.ServerManager.Spawn(pickup);
        pickup.GetComponent<RevolverPickup>().SetBullets(_bullets.Value);
        if (_boundInventory != null && _boundSlot >= 0)
            _boundInventory.ClearSlot(_boundSlot);
        if (Owner != null)
            TargetClearHeldObject(Owner);
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
    public void SaveBulletsToInventorySlot()
    {
        if (_boundInventory == null || _boundSlot < 0) return;
        
        if (_boundInventory.GetItemPrefabId(_boundSlot) != _boundPickupPrefabId)
            return;
        
        _boundInventory.UpdateSlotState(_boundSlot, System.BitConverter.GetBytes(_bullets.Value));
    }
    [Server]
    public void UnequipToInventory()
    {
        if (_boundInventory == null || _boundSlot < 0) return;
        SaveBulletsToInventorySlot();
        _playerController?.UnequipWeapon();
        if (Owner != null)
            TargetClearHeldObject(Owner);
        NetworkObject.Despawn();
    }
    [TargetRpc]
    private void TargetClearHeldObject(NetworkConnection target)
    {
        if (_playerController == null || !_playerController.IsOwner) return;
        Debug.Log("[Revolver] TargetClearHeldObject called on client");
        var pc = GetComponentInParent<PlayerController>();
        if (pc == null)
            pc = _playerController; // fallback
        if (pc != null)
        {
            var input = pc.GetComponent<PlayerInput>();
            if (input != null)
            {
                input.OnAttackEvent -= Shoot;
                input.OnDropRevolver -= Drop;
                Debug.Log("[Revolver] Unsubscribed from input events");
            }
            pc.UnequipWeapon();
            var pickup = pc.GetComponent<PickupController>();
            if (pickup != null)
                pickup.ClearHeld();
        }
        else
        {
            Debug.LogError("[Revolver] Could not find PlayerController for cleanup");
        }
    }
    [Server]
    public void DropToGround()
    {
        if (_playerController != null)
            _playerController.UnequipWeapon();
        NetworkObject pickup = Instantiate(revolverPickupPrefab, transform.position, transform.rotation);
        NetworkManager.ServerManager.Spawn(pickup);
        pickup.GetComponent<RevolverPickup>().SetBullets(_bullets.Value);

        if (_boundInventory != null && _boundSlot >= 0)
            _boundInventory.ClearSlot(_boundSlot);
        if (Owner != null)
            TargetClearHeldObject(Owner);
        NetworkObject.Despawn();
    }
    
    [ObserversRpc]
    private void PlayMuzzleFlashRpc(Vector3 pos, Quaternion rot)
    {
        if (muzzleFlashPrefab == null) return;
    
        GameObject fx = Instantiate(muzzleFlashPrefab, pos, rot);
    
        float lifetime = 0f;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            float psLifetime = main.duration;
        
            // Учитываем все режимы startLifetime
            switch (main.startLifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    psLifetime += main.startLifetime.constant;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    psLifetime += main.startLifetime.constantMax;
                    break;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    psLifetime += main.startLifetime.curveMax.Evaluate(1f);
                    break;
            }
        
            if (!main.loop) // зацикленные системы не учитываем
                lifetime = Mathf.Max(lifetime, psLifetime);
        }
    
        Destroy(fx, lifetime > 0f ? lifetime : 2f);
    }

}