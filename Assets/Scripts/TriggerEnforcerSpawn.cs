using System;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

public class TriggerEnforcerSpawn : NetworkBehaviour
{
    public EnforcerSpawner[] enforcerSpawners;
    public int n;

    private void OnEnable()
    {
        GameLogicEvents.OnTimerFinished += SpawnEnforcer;
    }

    private void OnDisable()
    {
        GameLogicEvents.OnTimerFinished -= SpawnEnforcer;
    }

    private void SpawnEnforcer()
    {
        foreach (EnforcerSpawner enforcer in enforcerSpawners)
            enforcer.SpawnTrainCar();
    }
}