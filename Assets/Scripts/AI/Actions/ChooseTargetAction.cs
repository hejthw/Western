using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using FishNet.Managing;
using FishNet;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "ChooseTarget",
    story: "[Self] Choose [Player]",
    category: "Action",
    id: "2e30f970b7521ed4f2c33b7df5b07694")]
public partial class ChooseTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;

    protected override Status OnStart()
    {
        // Выполняем только на сервере
        if (!InstanceFinder.IsServerStarted)
            return Status.Failure;

        GameObject target = FindNearestPlayer();

        if (target == null)
            return Status.Failure;

        Player.Value = target;
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd() { }

    private GameObject FindNearestPlayer()
    {
        if (Self.Value == null)
            return null;

        Vector3 selfPos = Self.Value.transform.position;
        GameObject nearest = null;
        float minDist = float.MaxValue;

        // Ищем все объекты с тегом "Player"
        // Альтернатива: FindObjectsByType<PlayerBehaviour>() если есть компонент-маркер
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            // Исключаем самого себя на случай если NPC тоже имеет тег
            if (player == Self.Value)
                continue;

            float dist = Vector3.Distance(selfPos, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }
}