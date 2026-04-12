using FishNet.Object;
using UnityEngine;

public class NetworkSoundManager : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundLibrary library;

    private void OnEnable()  => SoundBus.OnPlay += HandlePlay;
    private void OnDisable() => SoundBus.OnPlay -= HandlePlay;

    private void HandlePlay(SoundID id)
    {
        if (!IsOwner) return;
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
}