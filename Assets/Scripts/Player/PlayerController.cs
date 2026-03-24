using FishNet.Object;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public CinemachineCamera cinemachineCamera;

    [SerializeField] private PlayerPhysics physics;
    [SerializeField] private PlayerHealth health;

    [SerializeField] private PlayerInput input;

    [SerializeField] public Transform weaponHoldPoint;

    private bool _isDied;

    private Revolver _currentWeapon;
    private RevolverProjectile _currentGun;
    
    public bool IsArmed {get ; private set;}
    
    public void EquipGun(RevolverProjectile weapon)
    {
        _currentGun = weapon;
        IsArmed = true;
    }

    public void UnequipGun()
    {
        _currentGun = null;
        IsArmed = false;
    }
    
    public void EquipWeapon(Revolver weapon)
    {
        _currentWeapon = weapon;
        IsArmed = true;
    }

    public void UnequipWeapon()
    {
        _currentWeapon = null;
        IsArmed = false;
    }

    void Awake()
    {
        physics = GetComponent<PlayerPhysics>();
        health = GetComponent<PlayerHealth>();
        input = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        input.OnTestEvent += Test;
        PlayerEvents.OnDeadEvent += DisableMovement;
    }
    
    private void OnDisable()
    {
        input.OnTestEvent -= Test;
        PlayerEvents.OnDeadEvent -= DisableMovement;
    }

    private void DisableMovement(bool isDead)
    {
        physics.enabled = !isDead;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        cinemachineCamera.gameObject.SetActive(IsOwner);
        if (!IsOwner) DisableLocalComponents();
    }

    private void DisableLocalComponents()
    {
        GetComponent<PlayerInput>().enabled  = false;
        GetComponent<PlayerPhysics>().enabled = false;
        enabled = false;
    }

    private void Test()
    {
        if (!IsOwner) return;
        TakeDamageTest(10);
    }

    [ServerRpc]
    private void TakeDamageTest(int damage)
    {
        health.TakeDamage(damage);
    }
}
