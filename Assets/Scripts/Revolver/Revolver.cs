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
    private RevolverRecoil _recoil;
    
    private LayerMask _hitboxMask;
    
    [SerializeField] private BulletTrail bulletTrailPrefab;
    [SerializeField] private float trailMaxDistance = 100f; 

    private readonly SyncVar<int> _bullets = new SyncVar<int>(new SyncTypeSettings());
    
    private void Awake()
    {
        _hitboxMask = LayerMask.GetMask("Hitbox");
    }

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

        if (!playerNetObj.IsOwner) return;
        
        _recoil = GetComponent<RevolverRecoil>();
        _recoil?.Init(revolverData, controller);

        _input = playerNetObj.GetComponent<PlayerInput>();
        if (_input != null)
        {
            _input.OnAttackEvent += Shoot;
            _input.OnDropEvent   += Drop;
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
        
        _recoil?.ResetImmediate(); 
        
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
        if (_bullets.Value <= 0) { Debug.Log("No bullets"); return; }

        _recoil?.AddRecoil();

        Vector3 origin    = muzzle.position;
        Vector3 direction = muzzle.forward;

        ShootServerRpc(revolverData.damage, origin, direction);
        _fireTimer = revolverData.timeBeforeShot;
        
        SpawnTrail(origin, direction);
    }

    private void SpawnTrail(Vector3 origin, Vector3 direction)
    {
        if (bulletTrailPrefab == null) return;
        
        Vector3 endPoint = Physics.Raycast(origin, direction, out RaycastHit hit, trailMaxDistance)
            ? hit.point
            : origin + direction * trailMaxDistance;

        BulletTrail trail = Instantiate(bulletTrailPrefab, origin, Quaternion.identity);
        trail.Show(origin, endPoint);
    }
    
    [ServerRpc]
    private void ShootServerRpc(int damage, Vector3 pos, Vector3 dir)
    {
        if (_bullets.Value <= 0) return;
        _bullets.Value--;
        
        RaycastHit[] hits = Physics.RaycastAll(pos, dir, Mathf.Infinity,
            _hitboxMask, QueryTriggerInteraction.Collide);

        if (hits.Length == 0) return;
        
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            Debug.Log($"RaycastAll hit: {hit.transform.name} | Layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)}");

            if (hit.transform.TryGetComponent(out Hitbox hitbox))
            {
                int finalDamage = Mathf.RoundToInt(damage * hitbox.GetMultiplier());
                hitbox.OwnerHealth.TakeDamage(finalDamage);
                return;
            }
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