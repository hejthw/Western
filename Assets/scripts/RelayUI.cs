using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using TMPro;

public class RelayUI : MonoBehaviour
{
    [Header("UI элементы (TextMeshPro)")]
    public Button hostButton;
    public TextMeshProUGUI joinCodeText;
    public TMP_InputField joinInputField;
    public Button joinButton;
    public TextMeshProUGUI statusText;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }



    private void Start()
    {
        // === ВКЛЮЧАЕМ КУРСОР ПРИ ЗАПУСКЕ ===
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        if (joinCodeText) joinCodeText.text = "Код появится здесь...";
        if (statusText) statusText.text = "";
    }

    private async void OnHostClicked()
    {
        SetButtonsInteractable(false);
        if (statusText) statusText.text = "Создаём сессию...";

        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new SessionOptions { MaxPlayers = 4 }.WithFishyRelayNetwork();
            var session = await MultiplayerService.Instance.CreateSessionAsync(options);

            if (joinCodeText)
                joinCodeText.text = $"КОД: {session.Code}\n\nСкопируй и отправь друзьям!";

            if (statusText) statusText.text = "Сервер запущен! Ожидаем игроков...";

            Debug.Log($"✅ Код сессии: {session.Code}");
        }
        catch (System.Exception e)
        {
            if (statusText) statusText.text = $"Ошибка: {e.Message}";
            Debug.LogError(e);
        }
        finally
        {
            SetButtonsInteractable(true);
        }
    }

    private async void OnJoinClicked()
    {
        string code = joinInputField.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            if (statusText) statusText.text = "Введи код!";
            return;
        }

        SetButtonsInteractable(false);
        if (statusText) statusText.text = "Подключаемся...";

        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new JoinSessionOptions().WithFishyHandler();
            await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options);

            // === ПОСЛЕ УСПЕШНОГО ПОДКЛЮЧЕНИЯ ===
            if (statusText) statusText.text = "✅ Подключились! Игра начинается...";

            // Прячем меню и лочим курсор для FPS
            Invoke(nameof(StartGameplay), 0.5f); // небольшая задержка, чтобы FishNet успел заспавнить игрока
        }
        catch (System.Exception e)
        {
            if (statusText) statusText.text = $"Ошибка подключения: {e.Message}";
            Debug.LogError(e);
        }
        finally
        {
            SetButtonsInteractable(true);
        }
    }

    private void StartGameplay()
    {
        // Прячем весь UI
        if (canvas) canvas.enabled = false;   // или gameObject.SetActive(false);

        // Лочим курсор для нормального FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        hostButton.interactable = interactable;
        joinButton.interactable = interactable;
    }
}