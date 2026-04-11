using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NavigateToVector3", story: "[Self] navigates to [LastPlayerPos]", category: "Action", id: "85b8d503866904b54825ae5dcceb4ec6")]
public partial class NavigateToVector3Action : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Vector3> LastPlayerPos;

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
        if (_agent.pathPending)
            return Status.Running;

        // pathStatus проверяем — вдруг путь недостижим
        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return Status.Failure;

        float stopping = Mathf.Max(_stoppingDistance, 0.1f);

        if (_agent.remainingDistance <= stopping)
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

