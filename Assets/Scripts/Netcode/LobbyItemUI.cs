using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Вешается на префаб LobbyItem.
/// Подвяжи в инспекторе: PlayerImage, PlayerName, ReadyText, ReadyIndicator, ReadyButton.
/// </summary>
public class LobbyItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage playerImage;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text readyText;
    [SerializeField] private Image    readyIndicator;
    [SerializeField] private Button   readyButton;

    [Header("Colors")]
    [SerializeField] private Color readyColor    = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color notReadyColor = new Color(0.85f, 0.2f, 0.2f);

    private CSteamID         _steamId;
    private SteamLobbyManager _lobbyManager;
    private bool             _initialized;

    // ── Public ───────────────────────────────────────────────────────────────

    public void Init(CSteamID steamId, SteamLobbyManager lobbyManager)
    {
        _steamId      = steamId;
        _lobbyManager = lobbyManager;

        playerName.text = SteamFriends.GetFriendPersonaName(steamId);
        LoadAvatar(steamId);

        // Кнопка Ready — только у локального игрока, и только один раз
        if (readyButton != null)
        {
            bool isLocal = steamId == SteamUser.GetSteamID();
            readyButton.gameObject.SetActive(isLocal);

            if (isLocal && !_initialized)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(OnReadyClicked);
            }
        }

        _initialized = true;
        Refresh();
    }

    /// <summary>Вызывается контроллером при изменении готовности любого игрока.</summary>
    public void Refresh()
    {
        if (_lobbyManager == null) return;

        bool isReady = _lobbyManager.IsPlayerReady(_steamId);

        if (readyText != null)
            readyText.text = isReady ? "Готов" : "Не готов";

        if (readyIndicator != null)
            readyIndicator.color = isReady ? readyColor : notReadyColor;
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void OnReadyClicked()
    {
        bool current = _lobbyManager.IsPlayerReady(_steamId);
        _lobbyManager.SetLocalPlayerReady(!current);
    }

    private void LoadAvatar(CSteamID id)
    {
        if (playerImage == null) return;

        int handle = SteamFriends.GetMediumFriendAvatar(id);
        if (handle <= 0) return;

        if (!SteamUtils.GetImageSize(handle, out uint w, out uint h)) return;

        byte[] rgba = new byte[w * h * 4];
        if (!SteamUtils.GetImageRGBA(handle, rgba, rgba.Length)) return;

        Texture2D tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(rgba);
        FlipY(tex);
        tex.Apply();

        playerImage.texture = tex;
    }

    private static void FlipY(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        Color[] src = tex.GetPixels();
        Color[] dst = new Color[src.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                dst[y * w + x] = src[(h - 1 - y) * w + x];
        tex.SetPixels(dst);
    }
}