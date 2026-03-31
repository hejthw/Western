using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "PlayerDetected", story: "[Self] detected [Player]", category: "Conditions", id: "3bb1731b952e59258ba481ef09c48884")]
public partial class PlayerDetectedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;

    private Sensor _sensor;
    
    
    public override bool IsTrue()
    {
        if (_sensor.detectedObjects.Count > 0)
            {
            return true;
            }
        return false;
    }

    public override void OnStart()
    {
        _sensor = Self.Value.GetComponent<Sensor>();
    }

    public override void OnEnd()
    {
    }
}
