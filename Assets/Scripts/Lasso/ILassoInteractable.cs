public enum LassoInteractionType
{
    PullObject,     // Light
    PullPlayer,     // UnMovable
    PullCharacter,
    Custom
}

public interface ILassoInteractable
{
    LassoInteractionType GetInteractionType();

    void OnLassoAttach(LassoNetwork lasso);
    void OnLassoPull(LassoNetwork lasso);
    void OnLassoDetach(LassoNetwork lasso);
}