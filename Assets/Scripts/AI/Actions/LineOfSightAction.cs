using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "LineOfSight", story: "[Self] [HasLineOfSight] of [Player] on [LastPlayerTransform]", category: "Action", id: "314f67bfc7304c5726a22e7fae2d6aa6")]
public partial class LineOfSightAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> HasLineOfSight;
    [SerializeReference] public BlackboardVariable<GameObject> Player;

    private Transform _selfTransform;
    private Transform _playerTransform;

    private const float _losInterval = 0.2f;
    private float _losTimer;
    private bool _cachedLoS;
    
    protected override Status OnStart()
    {
        if (Self?.Value == null || Player?.Value == null)
            return Status.Failure;
        
        _selfTransform = Self.Value.transform;
        _playerTransform = Player.Value.transform;
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self?.Value == null || Player?.Value == null)
            return Status.Failure;

        _losTimer -= Time.deltaTime;
    
        if (_losTimer <= 0f)
        {
            _losTimer = _losInterval;
            _cachedLoS = CheckLineOfSight();
        }

        HasLineOfSight.Value = _cachedLoS;
    
        if (_cachedLoS) RotateSelf();
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }

    private bool CheckLineOfSight()
    {
        Vector3 origin = _selfTransform.position;
        Vector3 target = _playerTransform.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, Physics.AllLayers,QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject == Player.Value)
            {
#if UNITY_EDITOR
                Debug.DrawLine(origin, target, Color.green);
#endif
                HasLineOfSight.Value = true;
                RotateSelf();
                return true;
            }
        }
        
        HasLineOfSight.Value = false;
#if UNITY_EDITOR
        Debug.DrawLine(origin, target, Color.red);
#endif
        return false;
    }


    private void RotateSelf()
    {
        var target = Player.Value.transform;
        Self.Value.transform.LookAt(target);
    }

}

