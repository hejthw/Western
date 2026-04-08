using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NPCChoose", story: "[Self] choose [PlayerToAttack]", category: "Action", id: "539fd47222b9a59bb9cb41c3f3973b29")]
public partial class NpcChooseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Transform> PlayerToAttack;

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

