using UnityEngine;

public class MusicDirector : MonoBehaviour
{
    [SerializeField] private MusicProfile profile;
    [SerializeField] private AudioSource musicSource;

    public static MusicDirector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
    }

    public static void PlayGlobal(MusicCue cue, bool restartIfSame = false)
    {
        if (Instance == null)
            return;

        Instance.Play(cue, restartIfSame);
    }

    public static void StopGlobal()
    {
        if (Instance == null)
            return;

        Instance.musicSource.Stop();
    }

    public void Play(MusicCue cue, bool restartIfSame = false)
    {
        if (profile == null || musicSource == null)
            return;
        if (!profile.TryGetEntry(cue, out MusicProfile.Entry entry))
            return;
        if (entry.clip == null)
            return;

        if (!restartIfSame && musicSource.clip == entry.clip && musicSource.isPlaying)
            return;
        
        musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        musicSource.clip = entry.clip;
        musicSource.volume = entry.volume;
        musicSource.loop = entry.loop;
        musicSource.Play();
    }
}
