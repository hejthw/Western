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
    }
    
    private void OnDisable()
    {
        input.OnTestEvent -= Test;
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
