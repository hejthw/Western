using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HeistManager : NetworkBehaviour
{
    public static HeistManager Instance;

    private readonly SyncVar<int> totalValue = new SyncVar<int>();
    private readonly SyncVar<int> collectedValue = new SyncVar<int>();
    private readonly SyncVar<int> requiredValue = new SyncVar<int>();
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
            EndHeist();
    }

    [Server]
    public void EndHeist()
    {
        if (ended) return;

        ended = true;

        bool win = collectedValue.Value >= requiredValue.Value;

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

        Debug.Log(win ? "" : "");
        StartCoroutine(ReloadSceneAfterDelay(1.5f));
    }
    [ServerRpc]
    public void RequestEndHeist()
    {
        EndHeist();
    }
    
    public int GetCollected() => collectedValue.Value;
    public int GetRequired() => requiredValue.Value;

    private IEnumerator ReloadSceneAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene.name);
        else
            Application.Quit();
    }
}