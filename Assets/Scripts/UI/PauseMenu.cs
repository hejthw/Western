using System.Collections;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Меню паузы для мультиплеерной игры.
/// Подписывается на <see cref="PlayerInput.OnEscape"/> и показывает/скрывает панель паузы.
/// В мультиплеере игра не ставится на паузу (Time.timeScale остаётся 1),
/// вместо этого блокируется только локальный ввод игрока.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput _playerInput;

    [Header("Panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _confirmQuitPanel;

    [Header("Buttons — Pause")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _disconnectButton;   // покинуть сессию
    [SerializeField] private Button _quitToDesktopButton;

    [Header("Buttons — Settings")]
    [SerializeField] private Button _settingsBackButton;

    [Header("Buttons — Confirm Quit")]
    [SerializeField] private Button _confirmQuitYesButton;
    [SerializeField] private Button _confirmQuitNoButton;
    [SerializeField] private TextMeshProUGUI _confirmQuitText;

    [Header("HUD elements to hide on pause")]
    [SerializeField] private GameObject[] _hudElements;

    [Header("Animation")]
    [SerializeField] private float _fadeDuration = 0.18f;

    // ------------------------------------------------------------------ //

    private CanvasGroup _canvasGroup;
    private bool _isPaused;

    // Флаг: ждём подтверждения выхода на рабочий стол или дисконнекта
    private enum QuitTarget { Desktop, Disconnect }
    private QuitTarget _pendingQuitTarget;

    // ------------------------------------------------------------------ //

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SetGroupVisible(false, instant: true);
    }

    private void OnEnable()
    {
        if (_playerInput != null)
            _playerInput.OnEscape += TogglePause;

        _resumeButton?.onClick.AddListener(Resume);
        _settingsButton?.onClick.AddListener(OpenSettings);
        _disconnectButton?.onClick.AddListener(OnDisconnectClicked);
        _quitToDesktopButton?.onClick.AddListener(OnQuitToDesktopClicked);

        _settingsBackButton?.onClick.AddListener(CloseSettings);

        _confirmQuitYesButton?.onClick.AddListener(ConfirmQuit);
        _confirmQuitNoButton?.onClick.AddListener(CancelQuit);
    }

    private void OnDisable()
    {
        if (_playerInput != null)
            _playerInput.OnEscape -= TogglePause;

        _resumeButton?.onClick.RemoveListener(Resume);
        _settingsButton?.onClick.RemoveListener(OpenSettings);
        _disconnectButton?.onClick.RemoveListener(OnDisconnectClicked);
        _quitToDesktopButton?.onClick.RemoveListener(OnQuitToDesktopClicked);

        _settingsBackButton?.onClick.RemoveListener(CloseSettings);

        _confirmQuitYesButton?.onClick.RemoveListener(ConfirmQuit);
        _confirmQuitNoButton?.onClick.RemoveListener(CancelQuit);
    }

    // ------------------------------------------------------------------ //
    // Публичный API

    public void TogglePause()
    {
        Debug.Log("Toogle");
        if (_isPaused) Resume();
        else           Pause();
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        _pausePanel?.SetActive(true);
        _settingsPanel?.SetActive(false);
        _confirmQuitPanel?.SetActive(false);

        SetHudVisible(false);
        SetGroupVisible(true);

        // В мультиплеере Time.timeScale не трогаем — только блокируем ввод
        SetPlayerInputEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;

        SetGroupVisible(false);
        SetHudVisible(true);
        SetPlayerInputEnabled(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ------------------------------------------------------------------ //
    // Кнопки

    private void OpenSettings()
    {
        _pausePanel?.SetActive(false);
        _settingsPanel?.SetActive(true);
    }

    private void CloseSettings()
    {
        _settingsPanel?.SetActive(false);
        _pausePanel?.SetActive(true);
    }

    private void OnDisconnectClicked()
    {
        _pendingQuitTarget = QuitTarget.Disconnect;
        ShowConfirmQuit("Покинуть игровую сессию?");
    }

    private void OnQuitToDesktopClicked()
    {
        _pendingQuitTarget = QuitTarget.Desktop;
        ShowConfirmQuit("Выйти на рабочий стол?");
    }

    private void ShowConfirmQuit(string message)
    {
        if (_confirmQuitText != null)
            _confirmQuitText.text = message;

        _pausePanel?.SetActive(false);
        _confirmQuitPanel?.SetActive(true);
    }

    private void ConfirmQuit()
    {
        switch (_pendingQuitTarget)
        {
            case QuitTarget.Disconnect:
                Disconnect();
                break;

            case QuitTarget.Desktop:
                QuitToDesktop();
                break;
        }
    }

    private void CancelQuit()
    {
        _confirmQuitPanel?.SetActive(false);
        _pausePanel?.SetActive(true);
    }

    // ------------------------------------------------------------------ //
    // Мультиплеер

    private void Disconnect()
    {
        Resume();   // восстанавливаем курсор/ввод до смены сцены
        
        // Загрузить сцену главного меню (замените на своё имя сцены)
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void QuitToDesktop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ //
    // Вспомогательные методы

    /// <summary>
    /// Отключает/включает компонент PlayerInput на том же GameObject,
    /// чтобы во время паузы не обрабатывались игровые действия.
    /// </summary>
    private void SetPlayerInputEnabled(bool enabled)
    {
        if (_playerInput != null)
            _playerInput.enabled = enabled;
        
        Debug.Log("AAAAAAAAAAAAA");
    }

    private void SetHudVisible(bool visible)
    {
        foreach (GameObject hud in _hudElements)
            if (hud != null) hud.SetActive(visible);
    }

    private void SetGroupVisible(bool visible, bool instant = false)
    {
        StopAllCoroutines();

        if (instant)
        {
            _canvasGroup.alpha          = visible ? 1f : 0f;
            _canvasGroup.interactable   = visible;
            _canvasGroup.blocksRaycasts = visible;
            return;
        }

        StartCoroutine(FadeCanvasGroup(visible ? 1f : 0f, visible));
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, bool interactableOnEnd)
    {
        float start   = _canvasGroup.alpha;
        float elapsed = 0f;

        // Сразу включаем raycast при появлении
        if (interactableOnEnd)
            _canvasGroup.blocksRaycasts = true;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled — работает при timeScale = 0
            _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha          = targetAlpha;
        _canvasGroup.interactable   = interactableOnEnd;
        _canvasGroup.blocksRaycasts = interactableOnEnd;
    }
}