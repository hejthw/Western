using FishNet.Object;
using UnityEngine;

public class TestTrigger : NetworkBehaviour
{
    public EnforcerSpawner[] enforcerSpawners;
    public int n;
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            n += 1;
            if (n == 1)
            {
                Debug.Log("OnTriggerEnter");
                foreach (EnforcerSpawner enforcer in enforcerSpawners)
                    enforcer.SpawnTrainCar();
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("OnTriggerExit");
        }
    }
}