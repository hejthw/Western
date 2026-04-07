using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/DetectPlayer")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "DetectPlayer", message: "[Agent] has spotted [Player]", category: "Events", id: "f57c99436575d0a1a2241c6e6111c15f")]
public sealed partial class DetectPlayer : EventChannel<GameObject, GameObject> { }

