using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerControllerr : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public PlayerPhysicsBridge Physics { get; private set; }

    void Awake() => Physics = GetComponent<PlayerPhysicsBridge>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        cinemachineCamera.gameObject.SetActive(IsOwner);
        if (!IsOwner) DisableLocalComponents();
    }

    private void DisableLocalComponents()
    {
        GetComponent<PlayerInputBridge>().enabled  = false;
        GetComponent<PlayerPhysicsBridge>().enabled = false;
        enabled = false;
    }
}