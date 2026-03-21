using UnityEngine;
using Unity.Cinemachine;

public class HeadBob : MonoBehaviour
{
    [SerializeField] private PlayerPhysics pp;
    [SerializeField] private CinemachineBasicMultiChannelPerlin perlinNoise;
    [SerializeField] private CameraData data;

    private float _currentFrequency;
    private float _currentAmplitude;
    private float _targetFrequency;
    private float _targetAmplitude;

    private bool _isMoving;

    void Start()
    {
        perlinNoise.AmplitudeGain = 0f;
        perlinNoise.FrequencyGain = 0f;
    }

    void LateUpdate()
    {
        UpdateTargets();
        SmoothApply();
    }

    private void UpdateTargets()
    {
        bool walking = pp.CurrentState == PlayerState.STATE_WALK;
        bool sprinting = pp.CurrentState == PlayerState.STATE_SPRINT;
        _isMoving = walking || sprinting;

        if (sprinting)
        {
            _targetFrequency = data.sprintBobFrequency;
            _targetAmplitude = data.sprintBobAmplitude;
        }
        else if (walking)
        {
            _targetFrequency = data.walkBobFrequency;
            _targetAmplitude = data.walkBobAmplitude;
        }
        else
        {
            _targetFrequency = 0f;
            _targetAmplitude = 0f;
        }
    }

    private void SmoothApply()
    {
        float speed = _isMoving ? data.bobTransitionSpeed : data.settleSpeed;

        _currentAmplitude = Mathf.Lerp(_currentAmplitude, _targetAmplitude, Time.deltaTime * speed);
        _currentFrequency = Mathf.Lerp(_currentFrequency, _targetFrequency, Time.deltaTime * speed);

        perlinNoise.AmplitudeGain = _currentAmplitude;
        perlinNoise.FrequencyGain = _currentFrequency;
    }
}