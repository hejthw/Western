using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

public class HostGame : MonoBehaviour
{
    public async void StartHostWithRelay()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var options = new SessionOptions { MaxPlayers = 4 }
                        .WithFishyRelayNetwork();

        var session = await MultiplayerService.Instance.CreateSessionAsync(options);

        Debug.Log($" Хост запущен! Код для друзей: {session.Code}");
    }
}