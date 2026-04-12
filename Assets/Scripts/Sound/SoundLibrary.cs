using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "Audio/SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public SoundID id;
        public List<AudioClip> clips;
    }

    [SerializeField] private List<Entry> entries;

    private Dictionary<SoundID, AudioClip[]> _map;

    private void OnEnable() => Rebuild();

#if UNITY_EDITOR
    private void OnValidate() => Rebuild();
#endif

    private void Rebuild()
    {
        _map = new Dictionary<SoundID, AudioClip[]>(entries.Count);

        foreach (var e in entries)
        {
            if (_map.ContainsKey(e.id))
            {
                Debug.LogError($"[SoundLibrary] Дубликат SoundID.{e.id} в {name}.");
                continue;
            }

            var valid = e.clips?.FindAll(c => c != null);
            if (valid == null || valid.Count == 0) continue;

            _map[e.id] = valid.ToArray();
        }
    }

    public bool TryGetClip(SoundID id, out AudioClip clip)
    {
        if (_map == null) Rebuild();

        if (_map.TryGetValue(id, out var clips))
        {
            clip = clips[Random.Range(0, clips.Length)];
            return true;
        }

        clip = null;
        return false;
    }
}