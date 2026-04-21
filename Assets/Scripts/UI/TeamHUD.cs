using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamHUDEntry : MonoBehaviour
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
    private float _knockdownDuration;

    private Coroutine _healthLerpCoroutine;
    private Coroutine _knockTimerCoroutine;
    private bool _isKnocked;

    private PlayerHealth _trackedHealth;
    private PlayerName _trackedIdentity;

    public bool IsTracking(PlayerHealth player) => _trackedHealth == player;

    public void Track(PlayerHealth health, PlayerName identity, string playerName)
    {
        _trackedHealth    = health;
        _trackedIdentity  = identity;
        _knockdownDuration = data.knockoutDelay;

        nameText.text = string.IsNullOrEmpty(playerName) ? "..." : playerName;
        RefreshState(health.GetHealth());

        PlayerHealthEvents.OnTeammateHealthChange += OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange  += OnStateChanged;
        PlayerEvents.OnPlayerNameChanged          += OnNameChanged;
    }

    public void Untrack()
    {
        PlayerHealthEvents.OnTeammateHealthChange -= OnHealthChanged;
        PlayerHealthEvents.OnTeammateStateChange  -= OnStateChanged;
        PlayerEvents.OnPlayerNameChanged          -= OnNameChanged;

        _trackedHealth   = null;
        _trackedIdentity = null;
    }

    private void OnNameChanged(PlayerName identity, string name)
    {
        if (identity != _trackedIdentity) return;
        if (!string.IsNullOrEmpty(name))
            nameText.text = name;
    }

    private void OnHealthChanged(PlayerHealth player, int health)
    {
        if (player != _trackedHealth) return;
        RefreshState(health);
    }

    private void OnStateChanged(PlayerHealth player, PlayerHealthState state)
    {
        if (player != _trackedHealth) return;

        int hp = state switch
        {
            PlayerHealthState.Knockout => 0,
            PlayerHealthState.Dead     => -1,
            _                          => _trackedHealth.GetHealth()
        };

        RefreshState(hp);
    }

    private void RefreshState(int hp)
    {
        if (hp == -1)
            SetState(PlayerState.Dead);
        else if (hp == 0)
            SetState(PlayerState.Knock);
        else
        {
            SetState(PlayerState.Normal);
            SetHealthBarWidth(Mathf.Clamp01(hp / 100f) * MaxHealthBarWidth);
        }
    }

    private enum PlayerState { Normal, Knock, Dead }

    private void SetState(PlayerState state)
    {
        normalIcon.enabled = state == PlayerState.Normal;
        knockIcon.enabled  = state == PlayerState.Knock;
        deadIcon.enabled   = state == PlayerState.Dead;

        if (_knockTimerCoroutine != null) { StopCoroutine(_knockTimerCoroutine); _knockTimerCoroutine = null; }
        if (_healthLerpCoroutine != null) { StopCoroutine(_healthLerpCoroutine); _healthLerpCoroutine = null; }

        _isKnocked = state == PlayerState.Knock;

        if (_isKnocked)
            _knockTimerCoroutine = StartCoroutine(KnockdownTimer());
        else if (state == PlayerState.Dead)
            ApplyHealthBarWidth(0f);
    }

    private IEnumerator KnockdownTimer()
    {
        float elapsed = 0f;

        while (elapsed < _knockdownDuration)
        {
            elapsed += Time.deltaTime;
            float remaining = _knockdownDuration - elapsed;
            ApplyHealthBarWidth((remaining / _knockdownDuration) * MaxHealthBarWidth);
            yield return null;
        }

        ApplyHealthBarWidth(0f);
        _knockTimerCoroutine = null;
    }

    private void SetHealthBarWidth(float targetWidth)
    {
        if (_isKnocked) return;

        if (_healthLerpCoroutine != null)
            StopCoroutine(_healthLerpCoroutine);

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

    private void OnDestroy() => Untrack();
}