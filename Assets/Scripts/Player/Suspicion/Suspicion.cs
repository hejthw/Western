using System.Collections;
using FishNet.Object;
using UnityEngine;

public class Suspicion : NetworkBehaviour
{
    [SerializeField] private GameObject suspicionObject;
    [SerializeField] private float suspicionTime = 3.0f;
    [SerializeField] private SphereCollider suspicionCollider;
    [SerializeField] private float revolverColliderRadius = 5.0f;

    private float _defaultColliderRadius;

    void Awake()
    {
        suspicionObject.tag = "Untagged";
        _defaultColliderRadius = suspicionCollider.radius;
    }

    void OnEnable() => PlayerEvents.OnSuspicion += RaiseSuspicion;
    void OnDisable() => PlayerEvents.OnSuspicion -= RaiseSuspicion;

    void RaiseSuspicion(SuspicionType type)
    {
        if (IsOwner) SetSuspicionServerRpc(type);
    }

    [ServerRpc]
    private void SetSuspicionServerRpc(SuspicionType type)
    {
        StartCoroutine(Coroutine(type));
    }

    private IEnumerator Coroutine(SuspicionType type)
    {
        switch (type)
        {
            case SuspicionType.Lasso:
                SetTagClientRpc("Suspicion");
                break;

            case SuspicionType.RevolverShoot:
                SetTagAndColliderClientRpc("Suspicion", revolverColliderRadius);
                break;
        }

        yield return new WaitForSeconds(suspicionTime);

        switch (type)
        {
            case SuspicionType.Lasso:
                SetTagClientRpc("Untagged");
                break;

            case SuspicionType.RevolverShoot:
                SetTagAndColliderClientRpc("Untagged", _defaultColliderRadius);
                break;
        }
    }

    [ObserversRpc(BufferLast = true)]
    private void SetTagClientRpc(string newTag)
    {
        suspicionObject.tag = newTag;
    }

    [ObserversRpc(BufferLast = true)]
    private void SetTagAndColliderClientRpc(string newTag, float radius)
    {
        suspicionObject.tag = newTag;
        suspicionCollider.radius = radius;
    }
}