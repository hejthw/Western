using Unity.Cinemachine;
using FishNet.Object;
using UnityEngine;

public class PlayerNameplate : MonoBehaviour
{
    [SerializeField] private CinemachineCamera Camera;
    public Transform playerTransform;
    
    
    void LateUpdate()
    {
        if (Camera == null) return;
        
        transform.LookAt(playerTransform);
    }
}