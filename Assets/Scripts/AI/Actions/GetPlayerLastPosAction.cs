using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GetPlayerLastPos", story: "Get [LastPlayerPos] of [Player]", category: "Action", id: "d26e4e5be1d455fb725b6b87f2137bb9")]
public partial class GetPlayerLastPosAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> LastPlayerPos;
    [SerializeReference] public BlackboardVariable<GameObject> Player;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        LastPlayerPos.Value = Player.Value.transform.position;
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

