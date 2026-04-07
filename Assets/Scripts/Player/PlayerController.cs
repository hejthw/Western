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
    public bool IsLassoPulling { get; set; }
    // вынести отсюда
    private Rigidbody rb;
    private PlayerRotate playerRotate;
    [SerializeField] private float kinematicDelay = 4f;
    
    private bool _isDied;
    private bool movementDisabled = false;
    
    private Revolver _currentWeapon;
    public bool IsArmed {get ; private set;}
    
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

        PlayerHealthEvents.OnKnockoutEvent += DisableMovement;
    }
    
    private void OnDisable()
    {
        input.OnTestEvent -= Test;
        PlayerHealthEvents.OnKnockoutEvent -= DisableMovement;
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
        localUI.SetActive(IsOwner);
        if (!IsOwner) DisableLocalComponents();
        PlayerRegistry.Register(this);
    }

    private void DisableLocalComponents()
    {
        GetComponent<PlayerInput>().enabled  = false;
        GetComponent<PlayerPhysics>().enabled = false;
      
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
    public void SetLassoState(bool state)
    {
        IsLassoPulling = state;

   
        if (physics != null)
            physics.enabled = !state;

        if (playerRotate != null)
            playerRotate.enabled = !state;

      

        string who = IsServer ? "SERVER (host)" : "CLIENT";
        Debug.Log($"[{who}] Lasso state: {state} | Physics enabled: {!state}");
    }
}
