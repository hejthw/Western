using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public PlayerPhysics Physics { get; private set; }
    public PlayerHealth Health { get; private set; }
    
    public PlayerInput Input { get; private set; }

    void Awake()
    {
        Physics = GetComponent<PlayerPhysics>();
        Health = GetComponent<PlayerHealth>();
        Input = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        Input.OnTestEvent += Test;
    }
    
    private void OnDisable()
    {
        Input.OnTestEvent -= Test;
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
        Health.TakeDamage(damage);
    }
}
