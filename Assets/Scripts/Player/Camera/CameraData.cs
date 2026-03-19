using UnityEngine;

[CreateAssetMenu(fileName = "CameraData", menuName = "ScriptableObjects/CameraData")]
public class CameraData : ScriptableObject
{
    [Header("FOV")]
    public int normalFov = 80;
    public int sprintFov = 85;
    public float fovLerpDuration = 50f;
    
    [Header("HeadBob")]
    public bool enableHeadBob = true;
    public Vector3 headBobVector = new Vector3(0.001f, 0.08f, 0.01f);
    public float headBobSpeed = 5f;
}
