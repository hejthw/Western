public class Player
{
    public PlayerState ResolvePlayerState(
        bool hasInput,
        bool isGrounded,
        bool sprintHeld,
        float verticalVelocity)
    {
        if (!isGrounded)
            return verticalVelocity > 0.1f
                ? PlayerState.STATE_JUMP
                : PlayerState.STATE_FALL;

        if (!hasInput) return PlayerState.STATE_IDLE;
        if (sprintHeld) return PlayerState.STATE_SPRINT;
        return PlayerState.STATE_WALK;
    }
}