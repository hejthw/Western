using FishNet;
using FishySteamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamGameStartBootstrap : MonoBehaviour
{
    private static bool _pending;
    private static bool _shouldHost;
    private static string _hostAddress;
    private static string _targetScene;
    private static bool _consumed;
    private static bool _subscribed;

    public static void SetPendingStart(bool shouldHost, string hostAddress, string targetScene)
    {
        _pending = true;
        _shouldHost = shouldHost;
        _hostAddress = hostAddress;
        _targetScene = targetScene;
        _consumed = false;
        EnsureSubscribed();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInit()
    {
        EnsureSubscribed();
    }

    private static void EnsureSubscribed()
    {
        if (_subscribed)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        _subscribed = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_pending || _consumed)
            return;

        if (!scene.IsValid() || scene.name != _targetScene)
            return;

        if (InstanceFinder.NetworkManager == null)
        {
            Debug.LogError("[SteamStart] NetworkManager missing on gameplay scene.");
            return;
        }

        var transport = InstanceFinder.NetworkManager.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();
        if (transport == null)
        {
            Debug.LogError("[SteamStart] FishySteamworks transport missing.");
            return;
        }

        if (_shouldHost)
        {
            if (!InstanceFinder.ServerManager.Started)
                InstanceFinder.ServerManager.StartConnection();
            if (!InstanceFinder.ClientManager.Started)
                InstanceFinder.ClientManager.StartConnection();
            Debug.Log("[SteamStart] Host network started on gameplay scene.");
        }
        else
        {
            if (!ulong.TryParse(_hostAddress, out _))
            {
                Debug.LogError($"[SteamStart] Invalid host address '{_hostAddress}'.");
                return;
            }

            transport.SetClientAddress(_hostAddress);
            if (!InstanceFinder.ClientManager.Started)
                InstanceFinder.ClientManager.StartConnection();
            Debug.Log($"[SteamStart] Client connecting to host {_hostAddress}.");
        }

        _consumed = true;
    }
}
