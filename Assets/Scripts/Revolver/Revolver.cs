using System;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;

public class Revolver : NetworkBehaviour
{
    public RevolverData revolverData;
    [SerializeField] private PlayerInput input;
    [SerializeField] private Transform muzzle; 
    
    private float _fireTimer;

    private void OnEnable()
    {
        input.OnAttackEvent += Shoot;
    }
    
    private void OnDisable()
    {
        input.OnAttackEvent -= Shoot;
    }

    void Update()
    {
        Delay();
    }

    private void Shoot()
    {
        if (!IsOwner)
            return;

        if (_fireTimer <= 0f)
        {
            ShootServer(revolverData.damage, muzzle.position, muzzle.forward);
            _fireTimer = revolverData.timeBeforeShot;
            Debug.DrawRay(muzzle.position, muzzle.forward * 100f, Color.red, 2f);
        }
    }

    private void Delay()
    {
        if (_fireTimer > 0)
            _fireTimer -= Time.deltaTime;
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