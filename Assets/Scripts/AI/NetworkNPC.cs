using Unity.Behavior;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.AI;

public class NetworkNPC : NetworkBehaviour
{
    [SerializeField] private BehaviorGraphAgent _behaviorAgent;
    [SerializeField] private NavMeshAgent _navMeshAgent;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _behaviorAgent.enabled = true;
        _navMeshAgent.enabled = true;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!IsServerInitialized)
        {
            _behaviorAgent.enabled = false;
            _navMeshAgent.enabled = false;
        }
    }
    
    // private readonly SyncVar<CitizenStatus> _npcState = new SyncVar<CitizenStatus>();
}