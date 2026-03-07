using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

public class JoinGame : MonoBehaviour
{
    public async void JoinWithCode(string joinCode)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var options = new JoinSessionOptions()
                        .WithFishyHandler();

        var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, options);

        Debug.Log(" Успешно присоединились через Relay!");
    }
}