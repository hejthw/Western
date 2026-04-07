using System;
using FishNet.Object;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NPCDetect", story: "[Self] detects [Player] on [Tag]", category: "Action", id: "10d87f97e3c47b105ce04a786719395f")]
public partial class CitizenDetectAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<string> Tag;
    private NavMeshAgent _navAgent;
    private Sensor _sensor;
    private NetworkObject _networkObject;
    
    protected override Status OnStart()
    {
        _navAgent = Self.Value.GetComponent<NavMeshAgent>();
        _sensor = Self.Value.GetComponent<Sensor>();
        _networkObject = Self.Value.GetComponent<NetworkObject>();
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!_networkObject.IsServerInitialized) return Status.Running;
        
        var target = _sensor.GetClosestTarget(Tag);
        if (target == null) return Status.Running;
        
        Debug.Log($"Citizen detect: {target.name}");
        Player.Value = target.gameObject;
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

