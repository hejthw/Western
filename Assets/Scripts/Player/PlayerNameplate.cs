using Unity.Cinemachine;
using UnityEngine;

public class PlayerNameplate : MonoBehaviour
{
    [SerializeField] private CinemachineCamera Camera;

    void LateUpdate()
    {
        if (Camera == null) return;
        
        transform.LookAt(transform.position + Camera.transform.rotation * Vector3.forward, Camera.transform.rotation * Vector3.up);
    }
}