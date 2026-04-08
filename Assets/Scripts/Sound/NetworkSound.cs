// using FishNet.Object;
// using UnityEngine;
//
// public class NetworkSoundManager : NetworkBehaviour
// {
//     [SerializeField] private AudioSource audioSource;
//     
//     private AudioClip[] audioClips;
//
//     private void OnEnable()
//     
//     private void OnDisable()
//
//     private void MakeSound(int clipIndex)
//     {
//         PlayOnOwner(clipIndex);
//     }
//     
//     public void PlayOnOwner(int clipID)
//     {
//         // Сразу играем локально — без задержки
//         audioSource.PlayOneShot(clip);
//
//         // Отправляем команду на сервер
//         CmdRequestShootSound(clip);
//     }
//
//     // ─── ServerRpc: клиент → сервер ───
//     [ServerRpc]
//     private void CmdRequestShootSound(AudioClip clip)
//     {
//         // Сервер рассылает всем остальным наблюдателям
//         RpcPlayShootSoundOnObservers(clip);
//     }
//
//     // ─── ObserversRpc: сервер → все клиенты ───
//     // ExcludeOwner = true, чтобы НЕ играть повторно у владельца
//     [ObserversRpc(ExcludeOwner = true)]
//     private void RpcPlayShootSoundOnObservers(AudioClip clip)
//     {
//         audioSource.PlayOneShot(clip);
//     }
//
//     private void Play(int clipIndex)
// }