using System;
using FishNet.Object;
using UnityEngine;

public class NetworkSoundManager : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundLibrary library;
    private static NetworkSoundManager _localInstance;

    private void OnEnable()  => SoundBus.OnPlay += HandlePlay;
    private void OnDisable() => SoundBus.OnPlay -= HandlePlay;

    public void Awake()
    {
        audioSource.volume = PlayerPrefs.GetFloat("SoundVolume", 0.5f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            _localInstance = this;
    }
    
    public override void OnStopClient()
    {
        if (_localInstance == this)
            _localInstance = null;
        base.OnStopClient();
    }

    private void HandlePlay(SoundID id)
    {
        if (!IsOwner) return;
        audioSource.volume = PlayerPrefs.GetFloat("SoundVolume", 0.5f);
        PlayLocally(id);
        CmdRequestSound((int)id);
    }

    [ServerRpc]
    private void CmdRequestSound(int rawId) =>
        RpcPlayOnObservers(rawId);

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcPlayOnObservers(int rawId) =>
        PlayLocally((SoundID)rawId);

    private void PlayLocally(SoundID id)
    {
        if (library.TryGetClip(id, out var clip))
            audioSource.PlayOneShot(clip);
        else
            Debug.LogWarning($"[SoundManager] Клип для {id} не найден в библиотеке.");
    }
    
    public static void PlayImpactLocal(SoundID id)
    {
        if (_localInstance == null)
            return;
        
        _localInstance.PlayLocally(id);
    }
}