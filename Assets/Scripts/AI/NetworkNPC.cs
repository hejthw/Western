using Unity.Behavior;
using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;

public class NetworkNPC : NetworkBehaviour
{
    [SerializeField] private BehaviorGraphAgent _behaviorAgent;
    [SerializeField] private NavMeshAgent _navMeshAgent;

    // server-authority
    public override void OnStartServer()
    {
        base.OnStartServer();
        _behaviorAgent.enabled = true;
        _navMeshAgent.enabled = true;
    }
    
    // server-authority
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!IsServerInitialized)
        {
            _behaviorAgent.enabled = false;
            _navMeshAgent.enabled = false;
        }
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        _behaviorAgent.enabled = false;
        _navMeshAgent.enabled = false;
    }

    private void OnEnable() => NPCEvents.OnDeadEvent += Destroy;
    private void OnDisable() => NPCEvents.OnDeadEvent -= Destroy;

    private void Destroy()
    {
        ServerManager.Despawn(this);
    }
    
    // private readonly SyncVar<CitizenStatus> _npcState = new SyncVar<CitizenStatus>();
}