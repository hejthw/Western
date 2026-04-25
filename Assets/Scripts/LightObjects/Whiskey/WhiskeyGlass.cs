public class WhiskeyGlass : LightObject, IUsable
{
    public void Use() => PlayerEffectsEvents.RaiseWhiskeyUse();
    public string GetInteractLabel() => "Drink";
    
    protected override void OnHitPlayer(PlayerHealth playerHealth)
    {
        if (playerHealth.IsKnockedOut)
            playerHealth.RegisterReviveGlass();
    }
}