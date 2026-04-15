public class WhiskeyGlass : LightObject, IUsable
{
    public void Use() => PlayerEffectsEvents.RaiseWhiskeyUse();
}