using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public CinemachineCamera cinemachineCamera;

    [SerializeField] private PlayerPhysics physics;
    [SerializeField] private PlayerHealth health;

    [SerializeField] private PlayerInput input;

    [SerializeField] public Transform weaponHoldPoint;

    private bool _isDied;
    private bool movementDisabled = false;
    private Revolver _currentWeapon;
    
    public void EquipWeapon(Revolver weapon)
    {
        _currentWeapon = weapon;
    }

    public void UnequipWeapon()
    {
        _currentWeapon = null;
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
    public void DisableMovement()
    {
        if (physics != null)
            physics.enabled = false;
    }

    public void EnableMovement()
    {
        if (physics != null)
            physics.enabled = true;
    }

    [ServerRpc]
    private void TakeDamageTest(int damage)
    {
        health.TakeDamage(damage);
    }
}
