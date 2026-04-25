using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryIconDatabase", menuName = "UI/Inventory Icon Database")]
public class InventoryIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class IconEntry
    {
        public NetworkObject prefab;
        public Sprite icon;
    }

    [SerializeField] private List<IconEntry> entries = new List<IconEntry>();

    public bool TryGetIcon(NetworkObject prefab, out Sprite icon)
    {
        icon = null;
        if (prefab == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            IconEntry entry = entries[i];
            if (entry == null || entry.prefab == null || entry.icon == null)
                continue;

            if (entry.prefab == prefab)
            {
                icon = entry.icon;
                return true;
            }
        }

        return false;
    }
}
