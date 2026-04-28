using FishNet.Object;
using Steamworks;
using TMPro;
using UnityEngine;
using System.Collections;

public class PlayerNameView : NetworkBehaviour
{
    [SerializeField] private TMP_Text text;
    private PlayerName _playerName;
    private Coroutine _syncRoutine;
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        _playerName = GetComponentInParent<PlayerName>();

        if (IsOwner)
        {
            text.gameObject.SetActive(false);
            string playerName = SteamFriends.GetPersonaName();
            text.text = playerName;
            SetPlayerName(playerName);
        }
        else
        {
            text.text = string.Empty;
            _syncRoutine = StartCoroutine(SyncNameFromPlayerName());
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (_syncRoutine != null)
        {
            StopCoroutine(_syncRoutine);
            _syncRoutine = null;
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerName(string playerName)
    {
        SetPlayerNameForObservers(playerName);
    }


    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void SetPlayerNameForObservers(string playerName)
    {
        text.text = playerName;
    }

    private IEnumerator SyncNameFromPlayerName()
    {
        while (true)
        {
            if (_playerName == null)
                _playerName = GetComponentInParent<PlayerName>();

            if (_playerName != null && !string.IsNullOrEmpty(_playerName.SteamName))
            {
                if (text.text != _playerName.SteamName)
                    text.text = _playerName.SteamName;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
}