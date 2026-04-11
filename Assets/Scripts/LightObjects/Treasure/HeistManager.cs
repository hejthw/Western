using FishNet.Object;
using UnityEngine;

public class HeistManager : NetworkBehaviour
{
    public static HeistManager Instance;

    private int totalValue;
    private int collectedValue;
    private int requiredValue;
    [SerializeField] private HeistUI ui;
    private bool ended = false;

    private void Awake()
    {
        Instance = this;
       
    }
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        Debug.Log($"HeistManager started. Server: {IsServer}, Client: {IsClient}");
    }

    [Server]
    public void SetTotalValue(int value)
    {
        totalValue = value;
        requiredValue = Mathf.RoundToInt(value * 0.7f);

        RpcSyncValues(totalValue, collectedValue, requiredValue);
    }

    [Server]
    public void AddValue(int value)
    {
        if (ended) return;

        collectedValue += value;

        RpcSyncValues(totalValue, collectedValue, requiredValue);

        if (collectedValue >= totalValue)
            EndHeist();
    }

    [Server]
    public void EndHeist()
    {
        if (ended) return;

        ended = true;

        bool win = collectedValue >= requiredValue;

        RpcShowResult(win);
    }

    [ObserversRpc]
    private void RpcSyncValues(int total, int collected, int required)
    {
        totalValue = total;
        collectedValue = collected;
        requiredValue = required;

        Debug.Log($"UI: {required}");
    }

    [ObserversRpc]
    private void RpcShowResult(bool win)
    {

        Debug.Log(win ? "ÏÎÁÅÄÀ" : "ÏÎÐÀÆÅÍÈÅ");
    }
    [ServerRpc]
    public void RequestEndHeist()
    {
        EndHeist();
    }
    
    public int GetCollected() => collectedValue;
    public int GetRequired() => requiredValue;
}