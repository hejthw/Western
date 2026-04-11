using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Restart", story: "Restart [Self]", category: "Action", id: "bbb9772b8ea71b887166c9c4da41ca66")]
public partial class RestartAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    private BehaviorGraphAgent _selfAgent;

    protected override Status OnStart()
    {
        _selfAgent = Self.Value.GetComponent<BehaviorGraphAgent>();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
        _selfAgent.Restart();
    }
}

