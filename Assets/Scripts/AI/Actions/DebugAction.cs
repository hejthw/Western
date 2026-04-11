using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Debug", story: "Debug", category: "Action", id: "0155a6cce57926b21ad3aa06d2ca9491")]
public partial class DebugAction : Action
{

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        Debug.Log("Success");
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

