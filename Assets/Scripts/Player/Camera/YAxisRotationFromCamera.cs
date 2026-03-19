using FishNet.Object;
using UnityEngine;
using Unity.Cinemachine;

public class YAxisRotationFromCamera : NetworkBehaviour
{
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private Transform transform;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            enabled = false;
    }
    
    void Update()
    {
            float yaw = panTilt.PanAxis.Value;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}