using FishNet.Object;
using UnityEngine;

public class UnMovable : NetworkBehaviour, ILassoInteractable
{
    public void OnLassoAttach(LassoNetwork lasso)
    {
    }

    public void OnLassoPull(LassoNetwork lasso)
    {
        if (!IsServer) return;

        lasso.StartPullingPlayer(lasso.HitPoint);
    }

    public void OnLassoDetach(LassoNetwork lasso)
    {
    }

    public LassoInteractionType GetInteractionType()
    {
        return LassoInteractionType.PullPlayer;
    }
}