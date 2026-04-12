using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    [SerializeField] private PlayerPhysics physics;
    [SerializeField] private PlayerMovementData data;

    private PlayerState _previousState;
    private float _stepTimer;

    private void Update()
    {
        var state = physics.CurrentState;

        HandleStateChanged(state);
        HandleFootsteps(state);

        _previousState = state;
    }

    private void HandleStateChanged(PlayerState state)
    {
        if (state == _previousState) return;

        if (state == PlayerState.STATE_JUMP)
            SoundBus.Play(SoundID.PlayerJump);
    }

    private void HandleFootsteps(PlayerState state)
    {
        bool isMovingOnGround =
            state == PlayerState.STATE_WALK ||
            state == PlayerState.STATE_SPRINT;

        if (!isMovingOnGround)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= Time.deltaTime;

        if (_stepTimer <= 0f)
        {
            SoundID stepSound = state == PlayerState.STATE_SPRINT
                ? SoundID.PlayerFootstepSprint
                : SoundID.PlayerFootstepWalk;

            SoundBus.Play(stepSound);

            _stepTimer = state == PlayerState.STATE_SPRINT
                ? data.sprintStepInterval
                : data.walkStepInterval;
        }
    }
}