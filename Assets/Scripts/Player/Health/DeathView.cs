using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;

public class DeathView : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private CinemachineCamera spectatorCamera;

    [Header("Input Controllers")]
    [SerializeField] private CinemachineInputAxisController fpInputController;
    [SerializeField] private CinemachineInputAxisController spectatorInputController;

    [Header("Spectator Settings")]
    [SerializeField] private float followDistance = 4f;
    [SerializeField] private float followHeight = 1.5f;
    [SerializeField] private KeyCode nextTargetKey = KeyCode.Tab;
    [SerializeField] private KeyCode prevTargetKey = KeyCode.Q;

    [Header("Target Tag")]
    [SerializeField] private string playerTag = "Player";

    private CinemachineFollow _cinemachineFollow;
    private List<Transform> _targets = new();
    private int _currentTargetIndex = 0;
    private bool _isSpectating = false;

    private void Awake()
    {
        _cinemachineFollow = spectatorCamera.GetComponent<CinemachineFollow>();

        if (_cinemachineFollow != null)
        {
            _cinemachineFollow.FollowOffset = new Vector3(0f, followHeight, -followDistance);
        }

        ApplyState(false);
    }

    private void OnEnable() => PlayerEvents.OnDeadEvent += OnDead;
    private void OnDisable() => PlayerEvents.OnDeadEvent -= OnDead;

    private void OnDead(bool isDead) => ApplyState(isDead);

    private void Update()
    {
        if (!_isSpectating) return;

        if (Input.GetKeyDown(nextTargetKey))
            SwitchTarget(1);
        else if (Input.GetKeyDown(prevTargetKey))
            SwitchTarget(-1);
    }

    private void ApplyState(bool isDead)
    {
        _isSpectating = isDead;

        firstPersonCamera.Priority = isDead ? 0 : 10;
        spectatorCamera.Priority = isDead ? 10 : 0;

        fpInputController.enabled = !isDead;
        spectatorInputController.enabled = isDead;

        if (isDead)
        {
            RefreshTargets();
            SetTarget(_currentTargetIndex);
        }
    }

    private void RefreshTargets()
    {
        _targets = GameObject.FindGameObjectsWithTag(playerTag)
            .Where(go => go != gameObject)
            .Select(go => go.transform)
            .ToList();

        _currentTargetIndex = 0;
    }

    private void SwitchTarget(int direction)
    {
        if (_targets.Count == 0) return;
        
        _targets.RemoveAll(t => t == null);
        if (_targets.Count == 0) return;

        _currentTargetIndex = (_currentTargetIndex + direction + _targets.Count) % _targets.Count;
        SetTarget(_currentTargetIndex);
    }

    private void SetTarget(int index)
    {
        if (_targets.Count == 0 || index >= _targets.Count) return;

        Transform target = _targets[index];
        spectatorCamera.Follow = target;
        spectatorCamera.LookAt = target;
    }
}