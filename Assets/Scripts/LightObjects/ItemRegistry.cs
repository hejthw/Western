using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/ItemRegistry")]
public class ItemRegistry : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public int id;
        public NetworkObject prefab;
    }

    public List<Entry> items;

    private Dictionary<int, NetworkObject> _dict;

    public void Init()
    {
        _dict = new Dictionary<int, NetworkObject>();
        foreach (var e in items)
            _dict[e.id] = e.prefab;
    }

    public NetworkObject Get(int id)
    {
        return _dict.TryGetValue(id, out var obj) ? obj : null;
    }
}