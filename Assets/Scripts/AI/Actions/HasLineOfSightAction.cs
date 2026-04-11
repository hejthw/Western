using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HasLineOfSight", story: "[Self] has LineOfSight on [Player]", category: "Action", id: "ec61f0493fcdad47cc9e09402d8f56fa")]
public partial class HasLineOfSightAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self?.Value == null || Player?.Value == null)
            return Status.Failure;

        Vector3 origin = Self.Value.transform.position;
        Vector3 target = Player.Value.transform.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, Physics.AllLayers,QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject == Player.Value)
            {
                Debug.DrawLine(origin, target, Color.green);
                RotateSelf();
                return Status.Success;
            }
        }
        else
        {
            Debug.DrawLine(origin, target, Color.red);
        }

        return Status.Failure;
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

