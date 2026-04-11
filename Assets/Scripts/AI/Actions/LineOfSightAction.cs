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
    
    protected override Status OnStart()
    {
        _selfTransform = Self.Value.transform;
        _playerTransform = Player.Value.transform;
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self?.Value == null || Player?.Value == null)
            return Status.Failure;

        Vector3 origin = _selfTransform.position;
        Vector3 target = _playerTransform.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, Physics.AllLayers,QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject == Player.Value)
            {
                Debug.DrawLine(origin, target, Color.green);
                HasLineOfSight.Value = true;
                RotateSelf();
                return Status.Success;
            }
        }
        
        HasLineOfSight.Value = false;
        Debug.DrawLine(origin, target, Color.red);
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }


    private void RotateSelf()
    {
        var target = Player.Value.transform;
        Self.Value.transform.LookAt(target);
    }

}

