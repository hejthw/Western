using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Music Profile", fileName = "MusicProfile")]
public class MusicProfile : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public MusicCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        public bool loop;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    public bool TryGetEntry(MusicCue cue, out Entry entry)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].cue == cue)
            {
                entry = entries[i];
                return true;
            }
        }

        entry = default;
        return false;
    }
}
