using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerControllerr : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public PlayerPhysics Physics { get; private set; }

    void Awake() => Physics = GetComponent<PlayerPhysics>();

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
}