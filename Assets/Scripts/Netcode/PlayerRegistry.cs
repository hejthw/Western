using System.Collections.Generic;

public static class PlayerRegistry
{
    private static readonly List<PlayerController> Players = new();

    public static void Register(PlayerController p)   => Players.Add(p);
    public static void Unregister(PlayerController p) => Players.Remove(p);
    public static IReadOnlyList<PlayerController> All => Players;
}