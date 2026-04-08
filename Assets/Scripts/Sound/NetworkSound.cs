using FishNet.Object;
using UnityEngine;

public class NetworkSoundManager : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] clips; // массив всех звуков, порядок важен

    private void OnEnable()
    {
        PlayerEvents.OnShoot += HandleShoot;
    }

    private void OnDisable()
    {
        PlayerEvents.OnShoot -= HandleShoot;
    }

    private void HandleShoot(int clipIndex)
    {
        if (!IsOwner) return;
        PlayOnOwner(clipIndex);
    }

    public void PlayOnOwner(int clipIndex)
    {
        // Локально — мгновенно
        PlayClip(clipIndex);

        // Просим сервер разослать остальным
        CmdRequestSound(clipIndex);
    }

    [ServerRpc]
    private void CmdRequestSound(int clipIndex)
    {
        RpcPlayOnObservers(clipIndex);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void RpcPlayOnObservers(int clipIndex)
    {
        PlayClip(clipIndex);
    }

    private void PlayClip(int clipIndex)
    {
        if (clipIndex < 0 || clipIndex >= clips.Length) return;
        audioSource.PlayOneShot(clips[clipIndex]);
    }
}