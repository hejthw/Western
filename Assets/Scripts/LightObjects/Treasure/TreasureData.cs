using UnityEngine;

[System.Serializable]
public class TreasureData
{
    [Header("ID")]
    public string id;

    [Header("Visual")]
    public GameObject prefab;

    [Header("Gameplay")]
    public int value;

    [Tooltip("Ломается при столкновении")]
    public bool fragile;

    [Tooltip("Ломается от выстрелов")]
    public bool semiFragile;
}