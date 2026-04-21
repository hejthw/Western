using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
public class CashManager : NetworkBehaviour
{
    public static CashManager Instance;

    private readonly SyncVar<int> cash = new SyncVar<int>();
    private readonly SyncVar<int> maxCashAvailable = new SyncVar<int>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        RecalculateMaxCashFromScene();
    }

    [Server]
    public void AddCash(int amount)
    {
        cash.Value += amount;
        Debug.Log($"Cash: {cash.Value}");
    }

    public int GetCash() => cash.Value;
    public int GetMaxCashAvailable() => maxCashAvailable.Value;

    [Server]
    public void RecalculateMaxCashFromScene()
    {
        int total = 0;
        Treasure[] treasures = FindObjectsOfType<Treasure>();
        for (int i = 0; i < treasures.Length; i++)
            total += treasures[i].GetValue();

        maxCashAvailable.Value = Mathf.Max(total, 0);
    }
}