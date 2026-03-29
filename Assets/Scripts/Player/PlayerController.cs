using System.Collections;
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
    
    // вынести отсюда
    private Rigidbody rb;
    private PlayerRotate playerRotate;
    [SerializeField] private float kinematicDelay = 4f;
    
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
        rb = GetComponent<Rigidbody>();
        playerRotate = GetComponent<PlayerRotate>();
        
    }

    private void OnEnable()
    {
        input.OnTestEvent += Test;
        PlayerEvents.OnKnockoutEvent += DisableMovement;
    }
    
    private void OnDisable()
    {
        input.OnTestEvent -= Test;
        PlayerEvents.OnKnockoutEvent -= DisableMovement;
    }

    // вынести отсюда
    private void DisableMovement(bool isDead)
    {
        physics.enabled = !isDead;
        rb.freezeRotation = !isDead;
        playerRotate.enabled = !isDead;

        if (isDead)
            StartCoroutine(EnableKinematicDelayed());
        else
            rb.isKinematic = false;
    }

    // вынести отсюда
    private IEnumerator EnableKinematicDelayed()
    {
        yield return new WaitForSeconds(kinematicDelay);
        rb.isKinematic = true;
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
