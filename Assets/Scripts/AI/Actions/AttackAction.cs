using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack", story: "[Self] shoot at [Player]", category: "Action", id: "729d1f913bb42f778c7e0befc2da8db2")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<bool> HasLineOfSight;
    
    private Transform _selfTransform;
    private Transform _playerTransform;
    private float _fireTimer;

    private Enforcer _enforcer;
    private NPCAttackData _data;
    
    
}

