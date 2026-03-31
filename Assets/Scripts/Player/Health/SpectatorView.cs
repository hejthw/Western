using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class SpectatorView : MonoBehaviour
{
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private CinemachineCamera knockoutCamera;
    
    [SerializeField] private CinemachineInputAxisController fpInputController;
    [SerializeField] private CinemachineInputAxisController knockoutInputController;
    
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitHeight = 1.2f;
    
    [SerializeField] private Transform knockoutPivot;
    
    [SerializeField] private string playerTag = "Player";
    
    private Transform _knockoutJoint;
    private CinemachineOrbitalFollow _orbitalFollow;

    // Spectator
    private List<Transform> _spectatorTargets = new List<Transform>();
    private int _currentTargetIndex = 0;
    private Transform _currentSpectatorTarget;
    private bool _isInKnockout = false;
    private bool _isInDead = false;

    private void Awake()
    {
        _knockoutJoint = transform;
        _orbitalFollow = knockoutCamera.GetComponent<CinemachineOrbitalFollow>();
        ApplyState(false);
    }
    
    private void OnEnable()
    {
        PlayerHealthEvents.OnKnockoutEvent += OnKnockout;
        PlayerHealthEvents.OnDeadEvent += OnDead;
        PlayerEvents.NextTargetEvent += OnNextTarget;
        PlayerEvents.PrevTargetEvent += OnPrevTarget;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnKnockoutEvent -= OnKnockout;
        PlayerHealthEvents.OnDeadEvent -= OnDead;
        PlayerEvents.NextTargetEvent -= OnNextTarget;
        PlayerEvents.PrevTargetEvent -= OnPrevTarget;
    }

    private void OnNextTarget() => CycleTarget(1);
    private void OnPrevTarget() => CycleTarget(-1);

    private void LateUpdate()
    {
        Transform pivot = _isInKnockout && _currentSpectatorTarget != null
            ? _currentSpectatorTarget
            : _knockoutJoint;

        if (knockoutPivot != null && pivot != null)
        {
            knockoutPivot.position = pivot.position + Vector3.up * orbitHeight;
            knockoutPivot.rotation = Quaternion.identity;
        }
    }
    
    private void OnKnockout(bool isKnockout) => ApplyState(isKnockout);
    private void OnDead(bool isDead) => _isInDead = isDead;

    private void ApplyState(bool isKnockout)
    {
        _isInKnockout = isKnockout;
        
        firstPersonCamera.Priority = isKnockout ? 0 : 10;
        knockoutCamera.Priority    = isKnockout ? 10 : 0;

        fpInputController.enabled = !isKnockout;
        knockoutInputController.enabled = isKnockout;

        if (isKnockout && knockoutPivot != null)
        {
            RefreshSpectatorTargets();
            
            _currentSpectatorTarget = _spectatorTargets.Count > 0
                ? _spectatorTargets[0]
                : null;

            knockoutCamera.Follow = knockoutPivot;
            knockoutCamera.LookAt = knockoutPivot;

            if (_orbitalFollow != null)
                _orbitalFollow.Radius = orbitRadius;
        }
    }
    
    private void RefreshSpectatorTargets()
    {
        _spectatorTargets.Clear();
        _currentTargetIndex = 0;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (var p in players)
        {
            if (p.transform == _knockoutJoint) continue;
            _spectatorTargets.Add(p.transform);
        }
    }

    private void CycleTarget(int direction)
    {
        if (!_isInDead) return;
        Debug.Log("CycleTarget");
        RefreshSpectatorTargets();

        if (_spectatorTargets.Count == 0)
        {
            _currentSpectatorTarget = null;
            return;
        }

        _currentTargetIndex = (_currentTargetIndex + direction + _spectatorTargets.Count)
                              % _spectatorTargets.Count;

        _currentSpectatorTarget = _spectatorTargets[_currentTargetIndex];
    }
}