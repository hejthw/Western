using Unity.Cinemachine;
using FishNet.Object;
using UnityEngine;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera Camera;
    private Transform localPlayerTransform;

    void LateUpdate()
    {
        if (Camera == null) return;
        
        if (localPlayerTransform == null) return;
        transform.LookAt(localPlayerTransform.position);
    }
}