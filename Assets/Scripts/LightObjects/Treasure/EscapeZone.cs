using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;

public class EscapeZone : NetworkBehaviour
{
    public static EscapeZone Instance { get; private set; }

    private List<GameObject> playersInZone = new List<GameObject>();
    private float holdTimer = 0f;
    [SerializeField] private float requiredHoldTime = 3f;

    private readonly SyncVar<int> playersInZoneCount = new SyncVar<int>();
    private readonly SyncVar<int> playersTotalCount = new SyncVar<int>();
    private readonly SyncVar<bool> enoughCashCollected = new SyncVar<bool>();
    private readonly SyncVar<bool> canFinishNow = new SyncVar<bool>();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer) return;

        int totalPlayers = GetAlivePlayerCount();
        playersTotalCount.Value = totalPlayers;
        playersInZoneCount.Value = playersInZone.Count;

        int requiredCash = 0;
        if (HeistManager.Instance != null)
            requiredCash = HeistManager.Instance.GetRequired();

        int collectedCash = CashManager.Instance != null ? CashManager.Instance.GetCash() : 0;
        bool hasEnoughCash = collectedCash >= requiredCash && requiredCash > 0;
        enoughCashCollected.Value = hasEnoughCash;

        bool hasPlayerInZone = playersInZone.Count > 0;
        bool canTryFinish = hasEnoughCash && hasPlayerInZone;
        canFinishNow.Value = canTryFinish;

        if (!canTryFinish)
        {
            holdTimer = 0;
            return;
        }

        bool anyHolding = false;
        foreach (var player in playersInZone)
        {
            var input = player.GetComponent<PlayerInput>();
            if (input != null && input.IsHoldingFinish)
            {
                anyHolding = true;
                break;
            }
        }

        if (anyHolding)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= requiredHoldTime)
            {
                HeistManager.Instance.EndHeist(true);
            }
        }
        else
        {
            holdTimer = 0;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (!playersInZone.Contains(other.gameObject))
                playersInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            playersInZone.Remove(other.gameObject);
            holdTimer = 0;
        }
    }

    public bool IsFinishAvailable() => canFinishNow.Value;

    private int GetAlivePlayerCount()
    {
        int count = 0;
        var allPlayers = PlayerRegistry.All;
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i] != null && allPlayers[i].IsSpawned)
                count++;
        }

        return count;
    }
}