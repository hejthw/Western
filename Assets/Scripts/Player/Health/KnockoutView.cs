using UnityEngine;
using Unity.Cinemachine;


public class KnockoutView : MonoBehaviour
{
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private CinemachineCamera knockoutCamera;
    
    [SerializeField] private CinemachineInputAxisController fpInputController;
    [SerializeField] private CinemachineInputAxisController knockoutInputController;
    
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitHeight = 1.2f;
    
    [SerializeField] private Transform _knockoutPivot;
    private Transform _knockoutJoint;
    private CinemachineOrbitalFollow _orbitalFollow;

    private void Awake()
    {
        _knockoutJoint = transform;
        
        _orbitalFollow = knockoutCamera.GetComponent<CinemachineOrbitalFollow>();
        ApplyState(false);
    }

    private void LateUpdate()
    {
        if (_knockoutPivot != null && _knockoutJoint != null)
        {
            _knockoutPivot.position = _knockoutJoint.position + Vector3.up * orbitHeight;
            _knockoutPivot.rotation = Quaternion.identity;
        }
    }
    
    private void OnEnable()  => PlayerEvents.OnKnockoutEvent += OnKnockout;
    private void OnDisable() => PlayerEvents.OnKnockoutEvent -= OnKnockout;
    private void OnKnockout(bool isKnockout) => ApplyState(isKnockout);

    private void ApplyState(bool isKnockout)
    {
        firstPersonCamera.Priority  = isKnockout ? 0 : 10;
        knockoutCamera.Priority     = isKnockout ? 10 : 0;
        
        fpInputController.enabled = !isKnockout;
        
        knockoutInputController.enabled = isKnockout;
        
        if (isKnockout && _knockoutPivot != null)
        {
            knockoutCamera.Follow = _knockoutPivot;
            knockoutCamera.LookAt = _knockoutPivot;

            if (_orbitalFollow != null)
                _orbitalFollow.Radius = orbitRadius;
        }
    }
}