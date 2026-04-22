using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Вешается на префаб LobbyItem.
/// Подвяжи в инспекторе: PlayerImage, PlayerName, ReadyText, ReadyIndicator.
/// </summary>
public class LobbyItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage playerImage;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text readyText;
    [SerializeField] private Image readyIndicator;
    [SerializeField] private Button readyButton;

    [Header("Colors")]
    [SerializeField] private Color readyColor   = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color notReadyColor = new Color(0.85f, 0.2f, 0.2f);

    private CSteamID _steamId;
    private SteamLobbyManager _lobbyManager;
    private bool _isLocalPlayer;
    

    public void Init(CSteamID steamId, SteamLobbyManager lobbyManager)
    {
        _steamId     = steamId;
        _lobbyManager = lobbyManager;
        _isLocalPlayer = (steamId == SteamUser.GetSteamID());
        
        playerName.text = SteamFriends.GetFriendPersonaName(steamId);
        
        LoadAvatar(steamId);
        
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(_isLocalPlayer);
            if (_isLocalPlayer)
                readyButton.onClick.AddListener(OnReadyClicked);
        }

        Refresh();
    }
    
    public void Refresh()
    {
        bool isReady = _lobbyManager.IsPlayerReady(_steamId);

        readyText.text      = isReady ? "Готов" : "Не готов";
        readyIndicator.color = isReady ? readyColor : notReadyColor;
    }
    

    private void OnReadyClicked()
    {
        bool current = _lobbyManager.IsPlayerReady(_steamId);
        _lobbyManager.SetLocalPlayerReady(!current);
        Refresh();
    }

    private void LoadAvatar(CSteamID id)
    {
        if (playerImage == null) return;

        int handle = SteamFriends.GetMediumFriendAvatar(id);
        if (handle == -1 || handle == 0) return;

        if (!SteamUtils.GetImageSize(handle, out uint w, out uint h)) return;

        byte[] rgba = new byte[w * h * 4];
        if (!SteamUtils.GetImageRGBA(handle, rgba, rgba.Length)) return;

        Texture2D tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(rgba);
        
        FlipTextureVertically(tex);
        tex.Apply();

        playerImage.texture = tex;
    }

    private static void FlipTextureVertically(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        int w = tex.width, h = tex.height;
        Color[] flipped = new Color[pixels.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                flipped[y * w + x] = pixels[(h - 1 - y) * w + x];
        tex.SetPixels(flipped);
    }
}