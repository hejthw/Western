using System;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class Revolver : NetworkBehaviour
{
    public RevolverData revolverData;
    [SerializeField] private PlayerInput _input;
    
    [SerializeField] private Transform muzzle; 
    
    private float fireTimer;

    private void OnEnable()
    {
        _input.OnAttackEvent += Shoot;
    }
    
    private void OnDisable()
    {
        _input.OnAttackEvent -= Shoot;
    }

    void Update()
    {
        Delay();
    }

    private void Shoot()
    {
        if (!IsOwner)
            return;

        if (fireTimer <= 0f)
        {
            ShootServer(revolverData.damage, muzzle.position, muzzle.forward);
            fireTimer = revolverData.timeBeforeShot;
            Debug.DrawRay(muzzle.position, muzzle.forward * 100f, Color.red, 2f);
        }
    }

    private void Delay()
    {
        if (fireTimer > 0)
            fireTimer -= Time.deltaTime;
    }

    [ServerRpc]
    private void ShootServer(int damageToGive, Vector3 position, Vector3 direction)
    {
        if (Physics.Raycast(position, direction, out RaycastHit hit)
            && hit.transform.TryGetComponent(out PlayerHealth enemyHealth))
        {
            Debug.Log(hit.transform.name);
            enemyHealth.TakeDamage(damageToGive);
        }
    }
}