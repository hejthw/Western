using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using TMPro;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField] private PlayerHealthData data;
    
    [SerializeField] private TMP_Text nameText;
    
    [Header("Icons")]
    [SerializeField] private Image normalIcon;
    [SerializeField] private Image knockIcon;
    [SerializeField] private Image deadIcon;
    
    [Header("Health Bar")]
    [SerializeField] private Image healthBarImage;
    private const float MaxHealthBarWidth = 112f;
    private const float HealthBarLerpSpeed = 5f;
    private float KnockdownDuration;

    private float _targetHealthWidth;
    private Coroutine _healthLerpCoroutine;
    private Coroutine _knockTimerCoroutine;
    private bool _isKnocked;

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
        PlayerHealthEvents.OnLocalHealthChange -= UpdateHealthText;
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
        KnockdownDuration = data.knockoutDelay;
        string myName = SteamFriends.GetPersonaName();
        nameText.text = myName;
        SetHealthBarWidth(MaxHealthBarWidth, instant: true);
        SetState(PlayerState.Normal);
    }

    private void UpdateHealthText(int amount)
    {
        if (amount == -1)
        {
            SetState(PlayerState.Dead);
        }
        else if (amount == 0)
        {
            SetState(PlayerState.Knock);
        }
        else
        {
            SetState(PlayerState.Normal);
            float targetWidth = Mathf.Clamp01(amount / 100f) * MaxHealthBarWidth;
            SetHealthBarWidth(targetWidth);
        }
    }

    private enum PlayerState { Normal, Knock, Dead }

    private void SetState(PlayerState state)
    {
        normalIcon.enabled = state == PlayerState.Normal;
        knockIcon.enabled  = state == PlayerState.Knock;
        deadIcon.enabled   = state == PlayerState.Dead;

        bool knocked = state == PlayerState.Knock;

        if (_knockTimerCoroutine != null)
        {
            StopCoroutine(_knockTimerCoroutine);
            _knockTimerCoroutine = null;
        }

        if (_healthLerpCoroutine != null)
        {
            StopCoroutine(_healthLerpCoroutine);
            _healthLerpCoroutine = null;
        }

        _isKnocked = knocked;

        if (knocked)
        {
            _knockTimerCoroutine = StartCoroutine(KnockdownTimer());
        }
        else if (state == PlayerState.Dead)
        {
            ApplyHealthBarWidth(0f);
        }
    }

    private IEnumerator KnockdownTimer()
    {
        float elapsed = 0f;

        while (elapsed < KnockdownDuration)
        {
            elapsed += Time.deltaTime;
            float remaining = KnockdownDuration - elapsed;
            ApplyHealthBarWidth((remaining / KnockdownDuration) * MaxHealthBarWidth);
            yield return null;
        }

        ApplyHealthBarWidth(0f);
        _knockTimerCoroutine = null;
    }

    private void SetHealthBarWidth(float targetWidth, bool instant = false)
    {
        if (_isKnocked) return;

        _targetHealthWidth = targetWidth;

        if (_healthLerpCoroutine != null)
            StopCoroutine(_healthLerpCoroutine);

        if (instant)
            ApplyHealthBarWidth(targetWidth);
        else
            _healthLerpCoroutine = StartCoroutine(LerpHealthBar(targetWidth));
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