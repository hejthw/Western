using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
public class CashManager : NetworkBehaviour
{
    public static CashManager Instance;

    private readonly SyncVar<int> cash = new SyncVar<int>();

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void AddCash(int amount)
    {
        cash.Value += amount;
        Debug.Log($"Cash: {cash.Value}");
    }

    public int GetCash() => cash.Value;
}