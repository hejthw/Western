using FishNet.Object;
using UnityEngine;

public class TestTrigger : NetworkBehaviour
{
    public EnforcerSpawner[] enforcerSpawners;
    
    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter");
        foreach (EnforcerSpawner enforcer in enforcerSpawners)
            enforcer.SpawnTrainCar();
    }

    public void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit");
    }
}