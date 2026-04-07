public static class HitboxMultiplier
{
    public static float GetMultiplier(HitboxType hitboxType) => hitboxType switch
    {
        HitboxType.Head  => 1.0f,
        HitboxType.Torso => 0.5f,
        HitboxType.Arms => 0.25f,
        HitboxType.Legs => 0.25f,
        _ => 1.0f
    };
}
