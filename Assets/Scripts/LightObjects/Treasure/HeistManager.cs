using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

public class HeistManager : NetworkBehaviour
{
    public static HeistManager Instance;
    public static event Action<bool> HeistResultReceived;

    private readonly SyncVar<int> totalValue = new SyncVar<int>();
    private readonly SyncVar<int> collectedValue = new SyncVar<int>();
    private readonly SyncVar<int> requiredValue = new SyncVar<int>();
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

    private void Update()
    {
        if (!IsServerInitialized || ended)
            return;

        if (AreAllPlayersDead())
            EndHeist(false);
    }

    [Server]
    public void SetTotalValue(int value)
    {
        totalValue.Value = value;
        requiredValue.Value = Mathf.RoundToInt(value * 0.7f);

        RpcSyncValues(totalValue.Value, collectedValue.Value, requiredValue.Value);
    }

    [Server]
    public void AddValue(int value)
    {
        if (ended) return;

        collectedValue.Value += value;

        RpcSyncValues(totalValue.Value, collectedValue.Value, requiredValue.Value);

        if (collectedValue.Value >= totalValue.Value)
            EndHeist(true);
    }

    [Server]
    public void EndHeist()
    {
        bool win = collectedValue.Value >= requiredValue.Value;
        EndHeist(win);
    }

    [Server]
    public void EndHeist(bool win)
    {
        if (ended) return;

        ended = true;
        RpcShowResult(win);
    }

    [ObserversRpc]
    private void RpcSyncValues(int total, int collected, int required)
    {
        totalValue.Value = total;
        collectedValue.Value = collected;
        requiredValue.Value = required;

        Debug.Log($"UI: {required}");
    }

    [ObserversRpc]
    private void RpcShowResult(bool win)
    {
        Debug.Log(win ? "Heist win" : "Heist defeat");
        HeistResultReceived?.Invoke(win);
    }
    [ServerRpc]
    public void RequestEndHeist()
    {
        EndHeist();
    }
    
    public int GetCollected() => collectedValue.Value;
    public int GetRequired() => requiredValue.Value;

    private bool AreAllPlayersDead()
    {
        var allPlayers = PlayerRegistry.All;
        int trackedPlayers = 0;
        int aliveOrKnocked = 0;
        for (int i = 0; i < allPlayers.Count; i++)
        {
            PlayerController player = allPlayers[i];
            if (player == null || !player.IsSpawned)
                continue;
            trackedPlayers++;

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health == null)
                continue;

            if (health.GetHealth() >= 0)
                aliveOrKnocked++;
        }

        return trackedPlayers > 0 && aliveOrKnocked == 0;
    }
}