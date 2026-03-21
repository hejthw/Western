using UnityEngine;

[CreateAssetMenu(fileName = "CameraData", menuName = "ScriptableObjects/CameraData")]
public class CameraData : ScriptableObject
{
    [Header("FOV")]
    public int normalFov = 80;
    public int sprintFov = 85;
    public float fovLerpDuration = 50f;
    
    [Header("HeadBob")]
    public float walkBobFrequency = 1.4f;
    public float sprintBobFrequency = 2.2f;
    public float walkBobAmplitude = 0.3f;
    public float sprintBobAmplitude = 0.55f;
    [Header("HeadBob Smoothing")]
    public float bobTransitionSpeed = 8f;
    public float settleSpeed = 6f;
}
