using UnityEngine;

[CreateAssetMenu(menuName = "Heist/Loot Database")]
public class LootDatabase : ScriptableObject
{
    public TreasureData[] items;
}