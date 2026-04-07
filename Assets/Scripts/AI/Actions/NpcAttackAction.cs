using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NPCAttack", story: "[Self] attacks [Player]", category: "Action", id: "d061fb256cc4a53fc15eda8898839700")]
public partial class NpcAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    
    private float distance;
    private float hitChance;
    private PlayerHealth _currentPlayerHealth;
    
    protected override Status OnStart()
    {
        _currentPlayerHealth = Player.Value.GetComponent<PlayerHealth>();
        
        // TODO: ОТЬЕБАТЬСЯ ПОЛНОСТЬЮ ЕСЛИ ИГРОК В НОКЕ/УМЕР
        Debug.Log($"Current player health: {_currentPlayerHealth.GetHealth()}");
        if (_currentPlayerHealth.GetHealth() <= 0)
            return Status.Failure;
        
        distance = Vector3.Distance(Player.Value.transform.position, Self.Value.transform.position);
        hitChance = GetHitChance(distance,
            Player.Value.GetComponent<PlayerInput>().MoveInput != Vector2.zero);
        
        Debug.Log("Hit Chance on start: " + hitChance);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Random.value <= hitChance)
        {
            HitboxType hbType = ChooseBodyPart();
            Debug.Log($"Chosen part:{hbType}");
            int finalDamage = Mathf.RoundToInt(100f * HitboxMultiplier.GetMultiplier(hbType));
            _currentPlayerHealth.TakeDamage(finalDamage);
            return Status.Success;
        }
        return Status.Failure;
    }

    protected override void OnEnd()
    {
        Debug.Log($"Distance: {distance}");
        if (CurrentStatus == Status.Success)
            Debug.Log("SHOT");
        else Debug.Log("FAIL");
    }
    
        
    public HitboxType ChooseBodyPart()
    {
        var value = Random.value;

        if (value > 0.7f) return HitboxType.Arms;
        if (value > 0.25f) return HitboxType.Torso;
        return HitboxType.Head;
    }
    
    public float GetHitChance(float distance, bool playerIsMoving)
    {
        // TODO: УБРАТЬ ТРОЙКУ В ДАТА
        if (distance <= 3) return 0.75f;
        return playerIsMoving ? 0.3f : 0.6f;
    }
}

