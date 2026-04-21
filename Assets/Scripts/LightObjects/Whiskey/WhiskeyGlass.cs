public class WhiskeyGlass : LightObject, IUsable
{
    public void Use() => PlayerEffectsEvents.RaiseWhiskeyUse();
    
    protected override void OnHitPlayer(PlayerHealth playerHealth)
    {
        if (playerHealth.IsKnockedOut)
            playerHealth.RegisterReviveGlass();
    }
}