// using Unity.Cinemachine;
// using FishNet.Object;
// using UnityEngine;
//
// public class PlayerNameplate : MonoBehaviour
// {
//     [SerializeField] private CinemachineCamera Camera;
//     
//     
//     void LateUpdate()
//     {
//         if (Camera == null) return;
//
//         var localPlayerTransform = PlayerNameView.playerTransform.position;
//
//         if (localPlayerTransform == null) return;
//
//         var target = new Vector3(localPlayerTransform.x, transform.position.y, localPlayerTransform.z);
//         
//         transform.LookAt(target);
//     }
// }