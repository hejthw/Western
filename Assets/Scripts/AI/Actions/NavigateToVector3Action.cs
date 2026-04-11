using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NavigateToVector3", story: "[Self] navigates to [LastPlayerPos] until [HasLineOfSight]", category: "Action", id: "974f9e1dc39498eb2a72b6b058f4d0ff")]
public partial class NavigateToVector3Action : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Vector3> LastPlayerPos;
    [SerializeReference] public BlackboardVariable<bool> HasLineOfSight;

    private NavMeshAgent _agent;
    private Transform _selfTransform;
    private float _stoppingDistance = 0f;

    protected override Status OnStart()
    {
        if (Self?.Value == null)
            return Status.Failure;

        _agent = Self.Value.GetComponent<NavMeshAgent>();
        if (_agent == null)
            return Status.Failure;

        if (LastPlayerPos.Value == Vector3.zero)
            return Status.Failure;

        _selfTransform = Self.Value.transform;
        _agent.SetDestination(LastPlayerPos.Value);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (HasLineOfSight.Value)
        {
            _agent.ResetPath();
            return Status.Failure;
        }

        if (_agent.pathPending)
            return Status.Running;

        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return Status.Failure;

        if (_agent.remainingDistance <= Mathf.Max(_stoppingDistance, 0.1f))
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_agent != null)
        {
            _agent.ResetPath();
            _agent = null;
        }
        _selfTransform = null;
    }
}

