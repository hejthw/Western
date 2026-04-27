using System;
using FishNet;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UpdateTarget", story: "[Self] update [Player]", category: "Action", id: "b303962bd113214a527d578cf2277b83")]
public partial class UpdateTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    private float Interval = 1.5f;

    private float _timer;

    protected override Status OnStart()
    {
        if (!InstanceFinder.IsServerStarted)
            return Status.Failure;

        _timer = 0f;
        UpdateTarget(); // сразу при старте
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _timer += Time.deltaTime;

        if (_timer >= Interval)
        {
            _timer = 0f;
            UpdateTarget();
        }

        return Status.Running; // висит всегда, не завершается
    }

    protected override void OnEnd() { }

    private void UpdateTarget()
    {
        if (Self.Value == null) return;

        Vector3 selfPos = Self.Value.transform.position;
        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player == Self.Value) continue;

            float dist = Vector3.Distance(selfPos, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player;
            }
        }

        if (nearest != null && nearest != Player.Value)
        {
            Player.Value = nearest;
        }
    }
}

