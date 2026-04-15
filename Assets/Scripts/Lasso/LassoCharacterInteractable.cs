using FishNet.Object;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class LassoCharacterInteractable : NetworkBehaviour, ILassoInteractable
{
    [Header("Lasso Hold Settings")]
    public float holdDuration = 20f;
    public float maxHoldDistance = 25f;

    [Header("Yank Settings")]
    public float yankForce = 10f;

    [Header("Knockdown Settings")]
    public float knockdownDuration = 3f;

    [Header("References")]
    public Rigidbody rb;

    private LassoNetwork attachedLasso;
    private Coroutine holdCoroutine;
    private Coroutine knockdownCoroutine;
    private bool isKnockedDown;
    

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
    }

    // ─────────────────────────────────────────────
    //  ILassoInteractable
    // ─────────────────────────────────────────────

    public LassoInteractionType GetInteractionType() => LassoInteractionType.PullCharacter;

    public void OnLassoAttach(LassoNetwork lasso)
    {
        if (!IsServer) return;

        attachedLasso = lasso;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Останавливаем AI сразу при захвате
        RpcSetNPCAI(false);

        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);

        holdCoroutine = StartCoroutine(HoldRoutine(lasso));
    }

    public void OnLassoPull(LassoNetwork lasso)
    {
        if (!IsServer) return;
        if (isKnockedDown) return;

        if (rb == null) return;

        var playerNetObj = lasso.OwnerNetObj;
        if (playerNetObj == null) return;

        DropActiveItem();

        // Импульс в сторону игрока — аналогично OnLassoPull в LightObject
        Vector3 dir = (playerNetObj.transform.position - transform.position).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(dir * yankForce, ForceMode.Impulse);

        if (knockdownCoroutine != null)
            StopCoroutine(knockdownCoroutine);

        knockdownCoroutine = StartCoroutine(KnockdownRoutine());
    }

    public void OnLassoDetach(LassoNetwork lasso)
    {
        if (!IsServer) return;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        // Восстанавливаем AI только если не в нокдауне
        if (!isKnockedDown)
            RpcSetNPCAI(true);

        attachedLasso = null;
    }

    // ─────────────────────────────────────────────
    //  Server coroutines
    // ─────────────────────────────────────────────

    private IEnumerator HoldRoutine(LassoNetwork lasso)
    {
        float elapsed = 0f;

        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;

            if (lasso.Owner != null)
            {
                float dist = Vector3.Distance(transform.position, lasso.Owner.transform.position);
                if (dist > maxHoldDistance)
                {
                    Debug.Log("[LassoCharacter] Авто-отцепление: превышена дистанция");
                    lasso.ServerDetachAndReturn();
                    yield break;
                }
            }

            yield return null;
        }

        Debug.Log("[LassoCharacter] Авто-отцепление: истёк таймер");
        lasso.ServerDetachAndReturn();
    }

    private IEnumerator KnockdownRoutine()
    {
        isKnockedDown = true;
        RpcSetKnockdown(true);

        yield return new WaitForSeconds(knockdownDuration);

        isKnockedDown = false;
        RpcSetKnockdown(false);
        knockdownCoroutine = null;
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private void DropActiveItem()
    {
    }

    // ─────────────────────────────────────────────
    //  Observer RPCs
    // ─────────────────────────────────────────────

    [ObserversRpc]
    private void RpcSetKnockdown(bool knocked)
    {
        var pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            if (knocked) pc.DisableMovement();
            else pc.EnableMovement();
            pc.SetLassoState(knocked);
        }

        // NPC: при выходе из нокдауна восстанавливаем AI
        // (при входе — уже отключён с OnLassoAttach)
        if (!knocked)
        {
            var npc = GetComponent<NetworkNPC>();
            if (npc != null)
            {
                npc.EnableAI();
            }
                
        }
    }

    [ObserversRpc]
    private void RpcSetNPCAI(bool enabled)
    {
        var npc = GetComponent<NetworkNPC>();
        if (npc == null) return;

        if (enabled) npc.EnableAI();
        else npc.DisableAI();
    }
}