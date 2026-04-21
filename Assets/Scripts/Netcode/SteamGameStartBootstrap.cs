using FishNet;
using FishySteamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamGameStartBootstrap : MonoBehaviour
{
    private static bool _hasPendingStart;
    private static bool _shouldHost;
    private static string _hostAddress;
    private static string _targetScene;
    private static bool _consumed;

    public static void SetPendingStart(bool shouldHost, string hostAddress, string sceneName)
    {
        _hasPendingStart = true;
        _shouldHost = shouldHost;
        _hostAddress = hostAddress;
        _targetScene = sceneName;
        _consumed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TryApplyStartOnSceneLoad()
    {
        if (!_hasPendingStart || _consumed)
            return;

        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.name != _targetScene)
            return;

        if (InstanceFinder.NetworkManager == null)
        {
            Debug.LogError("[SteamStart] NetworkManager not found in gameplay scene.");
            return;
        }

        var transport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();
        if (transport == null)
        {
            Debug.LogError("[SteamStart] FishySteamworks transport not found.");
            return;
        }

        if (_shouldHost)
        {
            if (!InstanceFinder.ServerManager.Started)
                InstanceFinder.ServerManager.StartConnection();
            if (!InstanceFinder.ClientManager.Started)
                InstanceFinder.ClientManager.StartConnection();
            Debug.Log("[SteamStart] Host networking started on gameplay scene.");
        }
        else
        {
            transport.SetClientAddress(_hostAddress);
            if (!InstanceFinder.ClientManager.Started)
                InstanceFinder.ClientManager.StartConnection();
            Debug.Log($"[SteamStart] Client connecting to host {_hostAddress} on gameplay scene.");
        }

        _consumed = true;
    }
}
