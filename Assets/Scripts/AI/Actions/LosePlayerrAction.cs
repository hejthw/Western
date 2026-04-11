using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "LosePlayer", story: "[Self] lose [Player] if not [HasLineOfSight] in [LosingTime] seconds", category: "Action", id: "42a44854087ed091da3c6b943dcf8f9e")]
public partial class LosePlayerrAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<bool> HasLineOfSight;
    [SerializeReference] public BlackboardVariable<int> LosingTime;

    private float _timer;

    protected override Status OnStart()
    {
        _timer = LosingTime.Value;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (HasLineOfSight.Value)
        {
            // Увидели снова — сбрасываем таймер
            _timer = LosingTime.Value;
            return Status.Failure; // возвращаем Failure чтобы BT переключился на боевую ноду
        }

        _timer -= Time.deltaTime;

        if (_timer <= 0f)
            return Status.Success; // LoS не было всё время — потеряли игрока

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _timer = 0f;
    }
}

