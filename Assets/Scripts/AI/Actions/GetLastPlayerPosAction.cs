using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GetLastPlayerPos", story: "Get [LastPlayerPos]", category: "Action", id: "ef1b731d4966661c18de81e927bb43d7")]
public partial class GetLastPlayerPosAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> LastPlayerPos;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

