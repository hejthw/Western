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
    public float yankDistance = 4f;
    public float yankSpeed = 20f;

    [Header("Knockdown Settings")]
    public float knockdownDuration = 3f;

    [Header("References")]
    public Rigidbody rb;
    public Animator animator;

    private LassoNetwork attachedLasso;
    private Coroutine holdCoroutine;
    private Coroutine knockdownCoroutine;
    private bool isKnockedDown;
    

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
    }

    public LassoInteractionType GetInteractionType() => LassoInteractionType.PullCharacter;

    public void OnLassoAttach(LassoNetwork lasso)
    {
        if (!IsServer) return;

        attachedLasso = lasso;

        if (holdCoroutine != null)
            StopCoroutine(holdCoroutine);

        holdCoroutine = StartCoroutine(HoldRoutine(lasso));
    }

    public void OnLassoPull(LassoNetwork lasso)
    {
        if (!IsServer) return;
        if (isKnockedDown) return;

        Vector3 yankDirection = (lasso.Owner.transform.position - transform.position).normalized;
        StartCoroutine(YankAndKnockdownRoutine(yankDirection));
    }

    public void OnLassoDetach(LassoNetwork lasso)
    {
        if (!IsServer) return;

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        attachedLasso = null;
    }

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

    private IEnumerator YankAndKnockdownRoutine(Vector3 yankDirection)
    {
        DropActiveItem();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(yankDirection * yankSpeed, ForceMode.VelocityChange);
        }

        Vector3 startPos = transform.position;

        while (Vector3.Distance(startPos, transform.position) < yankDistance)
        {
            yield return null;
        }

        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        StartKnockdown();
    }

    private void StartKnockdown()
    {
        if (knockdownCoroutine != null)
            StopCoroutine(knockdownCoroutine);

        knockdownCoroutine = StartCoroutine(KnockdownRoutine());
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

    private void DropActiveItem()
    {
    }

    [ObserversRpc]
    private void RpcSetKnockdown(bool knocked)
    {
        if (animator != null)
            animator.SetBool("IsKnockedDown", knocked);

        var pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            if (knocked) pc.DisableMovement();
            else pc.EnableMovement();
            pc.SetLassoState(knocked);
        }

        var npc = GetComponent<NetworkNPC>();
        if (npc != null)
        {
            if (knocked) npc.DisableAI();
            else npc.EnableAI();
        }

        // Rigidbody: включаем/выключаем гравитацию и кинематику при нокдауне
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}