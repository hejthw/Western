using UnityEngine;
using Unity.Cinemachine;

public class PlayerRotationFromCamera : MonoBehaviour
{
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private Transform playerModel;

    void Update()
    {
        float yaw = panTilt.PanAxis.Value;
        playerModel.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}