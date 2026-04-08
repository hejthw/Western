using System.Collections;
using FishNet.Object;
using Steamworks;
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
    [SerializeField] private GameObject localUI;
    [SerializeField] private PlayerNameView playerNameView;
    
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
        PlayerEvents.UpdateName += UpdateName;
    }
    
    private void OnDisable()
    {
        input.OnTestEvent -= Test;
        PlayerHealthEvents.OnKnockoutEvent -= DisableMovement;
        PlayerEvents.UpdateName -= UpdateName;
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

    public override void OnStopClient()
    {
        PlayerRegistry.Unregister(this);
    }

    [ServerRpc]
    private void UpdateName()
    {
        name = playerNameView.PlayerName.Value;
    }
}
