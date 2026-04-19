using System;
using Unity.Behavior;
using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;

public class NetworkNPC : NetworkBehaviour
{
    [SerializeField] private BehaviorGraphAgent _behaviorAgent;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private Rigidbody _rigidbody;
    
    // server-authority
    public override void OnStartServer()
    {
        base.OnStartServer();
        _behaviorAgent.enabled = true;
        _navMeshAgent.enabled = true;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        _rigidbody.isKinematic = true;
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
    
    public void EnableAI()
    {
        Debug.Log("EnableAI");
        if (!IsServerInitialized) return;
        _behaviorAgent.enabled = true;
        _navMeshAgent.enabled = true;
        _rigidbody.isKinematic = true;
    }
    
    public void DisableAI()
    {
        Debug.Log("DisableAI");
        if (!IsServerInitialized) return;
        _behaviorAgent.enabled = false;
        _navMeshAgent.enabled = false;
        _rigidbody.isKinematic = false;
    }
    
    
    
    // private readonly SyncVar<CitizenStatus> _npcState = new SyncVar<CitizenStatus>();
}