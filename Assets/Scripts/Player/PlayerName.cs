using FishNet.Object;
using FishNet.Object.Synchronizing;
using Steamworks;
using UnityEngine;

public class PlayerName : NetworkBehaviour
{
    private readonly SyncVar<string> _steamName = new SyncVar<string>();

    public string SteamName => _steamName.Value;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _steamName.OnChange += OnNameChanged;
    }
    

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _steamName.OnChange -= OnNameChanged;
    }

    [ServerRpc]
    private void CmdSetName(string name)
    {
        _steamName.Value = name;
    }

    private void OnNameChanged(string prev, string next, bool asServer)
    {
        if (asServer || IsOwner) return;
        PlayerEvents.RaisePlayerNameChanged(this, next);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            CmdSetName(SteamFriends.GetPersonaName());
        if (!IsOwner)
            PlayerEvents.RaisePlayerRegistered(this, _steamName.Value);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        if (!IsOwner)
            PlayerEvents.RaisePlayerUnregistered(this);
    }
}