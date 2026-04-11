using System.Collections;
using FishNet.Object;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class Suspicion : NetworkBehaviour
{
    [SerializeField] private GameObject suspicionObject;
    [SerializeField] private float suspicionTime = 3.0f;

    void Awake()
    {
        suspicionObject.tag = "Untagged";
    }

    void OnEnable() => PlayerEvents.OnSuspicion += RaiseSuspicion;
    void OnDisable() => PlayerEvents.OnSuspicion -= RaiseSuspicion;

    void RaiseSuspicion()
    {
        if (IsOwner) SetSuspicionServerRpc();
    }
    
    [ServerRpc]
    private void SetSuspicionServerRpc()
    {
        StartCoroutine(Coroutine());
    }
    
    private IEnumerator Coroutine()
    {
        SetTagClientRpc("Suspicion");
        yield return new WaitForSeconds(suspicionTime);
        SetTagClientRpc("Untagged");
    }

    [ObserversRpc(BufferLast = true)]
    private void SetTagClientRpc(string newTag)
    {
        suspicionObject.tag = newTag;
    }
}
