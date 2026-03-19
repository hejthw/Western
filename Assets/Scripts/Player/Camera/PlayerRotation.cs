using FishNet.Object;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerRotationFromCamera : NetworkBehaviour
{
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private Transform playerModel;

    void Update()
    {
        if (IsOwner)
        {
            float yaw = panTilt.PanAxis.Value;
            playerModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
        
    }
}