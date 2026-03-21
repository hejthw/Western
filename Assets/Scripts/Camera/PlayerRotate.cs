using FishNet.Object;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerRotate : NetworkBehaviour
{
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private Transform transform;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            enabled = false;
    }
    
    void FixedUpdate()
    {
        float yaw = panTilt.PanAxis.Value;
        float pitch = panTilt.TiltAxis.Value;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}