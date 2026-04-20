using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    
    [Header("Health Bar")]
    [SerializeField] private Image healthBarImage;
    private const float MaxHealthBarWidth = 112f;
    private const float HealthBarLerpSpeed = 5f;

    private float _targetHealthWidth;
    private Coroutine _healthLerpCoroutine;
    
    [Header("Team HUD")]
    [SerializeField] private Transform teamHUDContainer;
    [SerializeField] private TeamHUDEntry teamEntryPrefab;

    private readonly Dictionary<PlayerName, TeamHUDEntry> _teamEntries = new();

    private void OnEnable()
    {
        PlayerHealthEvents.OnLocalHealthChange += UpdateHealthText;
        PlayerEvents.OnPlayerRegistered += OnTeammateJoined;
        PlayerEvents.OnPlayerUnregistered += OnTeammateLeft;
    }

    private void OnDisable()
    {
        PlayerHealthEvents.OnLocalHealthChange  -= UpdateHealthText;
        PlayerEvents.OnPlayerRegistered -= OnTeammateJoined;
        PlayerEvents.OnPlayerUnregistered -= OnTeammateLeft;
    }

    public override void OnStartClient()
    {
        if (!IsOwner)
            gameObject.SetActive(false);
    }

    private void OnTeammateJoined(PlayerName identity, string name)
    {
        // PlayerHealth лежит на том же GameObject
        var health = identity.GetComponent<PlayerHealth>();
        if (health == null) return;

        var entry = Instantiate(teamEntryPrefab, teamHUDContainer);
        entry.Track(health, identity, name);
        _teamEntries[identity] = entry;
    }

    private void OnTeammateLeft(PlayerName identity)
    {
        if (!_teamEntries.TryGetValue(identity, out var entry)) return;

        entry.Untrack();
        Destroy(entry.gameObject);
        _teamEntries.Remove(identity);
    }
    
    public void Awake()
    {
        string myName = SteamFriends.GetPersonaName();
        nameText.text = myName;
        healthText.text = "100";
        SetHealthBarWidth(MaxHealthBarWidth, instant: true);
    }

    private void UpdateHealthText(int amount)
    {
        if (amount == -1)
        {
            healthText.text = "Dead";
            SetHealthBarWidth(0f);
        }
        else if (amount != 0)
        {
            healthText.text = amount.ToString();
            float targetWidth = Mathf.Clamp01(amount / 100f) * MaxHealthBarWidth;
            SetHealthBarWidth(targetWidth);
        }
        else
        {
            healthText.text = "Knock";
            SetHealthBarWidth(0f);
        }
    }
    
    private void SetHealthBarWidth(float targetWidth, bool instant = false)
    {
        _targetHealthWidth = targetWidth;

        if (_healthLerpCoroutine != null)
            StopCoroutine(_healthLerpCoroutine);

        if (instant)
        {
            ApplyHealthBarWidth(targetWidth);
        }
        else
        {
            _healthLerpCoroutine = StartCoroutine(LerpHealthBar(targetWidth));
        }
    }

    private IEnumerator LerpHealthBar(float targetWidth)
    {
        RectTransform rt = healthBarImage.rectTransform;

        while (!Mathf.Approximately(rt.sizeDelta.x, targetWidth))
        {
            float newWidth = Mathf.Lerp(rt.sizeDelta.x, targetWidth, Time.deltaTime * HealthBarLerpSpeed);
            ApplyHealthBarWidth(newWidth);
            yield return null;
        }

        ApplyHealthBarWidth(targetWidth);
        _healthLerpCoroutine = null;
    }

    private void ApplyHealthBarWidth(float width)
    {
        RectTransform rt = healthBarImage.rectTransform;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
    }
}