using UnityEngine;
using Unity.Cinemachine;

public class RevolverRecoil : MonoBehaviour
{
    private RevolverData _data;
    private CinemachinePanTilt _panTilt;
    
    private float _appliedPitch;
    private float _targetPitch;
    
    private float _appliedYaw;
    private float _targetYaw;
    
    private int _shotsFired;
    private float _resetTimer;

    public void Init(RevolverData data, PlayerController controller)
    {
        _data = data;
        _panTilt = controller.cinemachineCamera.GetComponent<CinemachinePanTilt>();
    }

    public void AddRecoil()
    {
        _shotsFired++;
        _resetTimer = _data.resetDelay;

        float upKick;
        float sideKick;

        if (_shotsFired <= 1)
        {
            upKick = _data.recoilUp;
            sideKick = Random.Range(-_data.recoilSideMax, _data.recoilSideMax);
        }
        else
        {
            float spray = Mathf.Min(_shotsFired, 6);
            float multi = 1f + (spray - 1f) * (_data.sprayMultiplier - 1f) / 5f;
            upKick = _data.recoilUp * multi;
            sideKick = Random.Range(-_data.sprayRandomness, _data.sprayRandomness) * multi;
        }

        _targetPitch -= upKick;
        _targetYaw += sideKick;
        
        _targetPitch = Mathf.Clamp(_targetPitch, -25f, 0f);
    }

    private void Update()
    {
        if (_data == null || _panTilt == null) return;
        
        if (_resetTimer > 0f)
        {
            _resetTimer -= Time.deltaTime;
            if (_resetTimer <= 0f)
                _shotsFired = 0;
        }

        _targetPitch = Mathf.Lerp(_targetPitch, 0f, Time.deltaTime * _data.recoverySpeed);
        _targetYaw = Mathf.Lerp(_targetYaw, 0f, Time.deltaTime * _data.recoverySpeed);
        
        float newPitch = Mathf.Lerp(_appliedPitch, _targetPitch, Time.deltaTime * _data.recoilApplySpeed);
        float newYaw = Mathf.Lerp(_appliedYaw, _targetYaw, Time.deltaTime * _data.recoilApplySpeed);
        
        float deltaPitch = newPitch - _appliedPitch;
        float deltaYaw = newYaw - _appliedYaw;

        _panTilt.TiltAxis.Value += deltaPitch;
        _panTilt.PanAxis.Value += deltaYaw;
        
        _appliedPitch = newPitch;
        _appliedYaw = newYaw;
    }

    public void ResetImmediate()
    {
        if (_panTilt != null)
            _panTilt.TiltAxis.Value -= _appliedPitch;

        _appliedPitch = 0f;
        _targetPitch = 0f;
        _shotsFired = 0;
        _resetTimer = 0f;
    }
}