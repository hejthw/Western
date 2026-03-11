public class PlayerJumpLogic
{
    private readonly PlayerMovementData _data;
    private bool _jumpRequested;

    public PlayerJumpLogic(PlayerMovementData data) => _data = data;
    
    public void RequestJump() => _jumpRequested = true;
    
    public float? TryConsume(bool isGrounded)
    {
        if (!_jumpRequested || !isGrounded) return null;
        _jumpRequested = false;
        return _data.jumpForce;
    }

    public void Reset() => _jumpRequested = false;
}