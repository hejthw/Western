using UnityEngine;
using System.Reflection;
using System.Collections;

/// <summary>
/// Интегрирует CashHUD с системой задач для отображения событий таймера
/// </summary>
[RequireComponent(typeof(CashHUD))]
public class CashHUDTaskIntegration : MonoBehaviour
{
    [Header("Task Integration")]
    [Tooltip("ID события при окончании таймера")]
    public string timerFinishedEventId = "heist_timer_finished";
    
    [Tooltip("ID события при открытии двери ограбления")]
    public string doorOpenedEventId = "heist_door_opened";
    
    [Tooltip("Коллекция событий")]
    public TaskEventCollection eventCollection;
    
    [Tooltip("Автоматически найти TaskManager")]
    public bool autoFindTaskManager = true;

    [Header("Timer Override")]
    [Tooltip("Переопределить поведение таймера CashHUD")]
    public bool overrideTimerBehavior = true;
    
    [Tooltip("Показывать дополнительные логи")]
    public bool enableDebugLogs = true;

    [Header("Custom Timer End Message")]
    [Tooltip("Использовать кастомный текст вместо события из коллекции")]
    public bool useCustomTimerEndText = false;
    
    [TextArea(2, 4)]
    [Tooltip("Кастомный текст при окончании таймера")]
    public string customTimerEndText = "Время вышло! Быстро к выходу!";
    
    [Tooltip("Цвет текста при окончании таймера")]
    public Color timerEndTextColor = Color.red;
    
    [Tooltip("Размер шрифта (0 = использовать по умолчанию)")]
    [Range(0, 72)]
    public int timerEndFontSize = 22;

    private CashHUD cashHUD;
    private TaskManager taskManager;
    private bool hasOverriddenTimer = false;
    private Coroutine customTimerCoroutine;

    private void Awake()
    {
        cashHUD = GetComponent<CashHUD>();
        
        if (cashHUD == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[CashHUDTaskIntegration] CashHUD component not found!");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (autoFindTaskManager)
        {
            taskManager = FindObjectOfType<TaskManager>();
            
            if (taskManager == null && enableDebugLogs)
                Debug.LogWarning("[CashHUDTaskIntegration] TaskManager not found!");
        }

        // Подписываемся на события
        HeistDoor.OpenedByLocalPlayer += OnHeistDoorOpened;
    }

    private void OnDestroy()
    {
        HeistDoor.OpenedByLocalPlayer -= OnHeistDoorOpened;
        
        if (customTimerCoroutine != null)
        {
            StopCoroutine(customTimerCoroutine);
            customTimerCoroutine = null;
        }
    }

    private void OnHeistDoorOpened()
    {
        // Активируем событие открытия двери
        if (!string.IsNullOrEmpty(doorOpenedEventId))
        {
            ActivateTaskEvent(doorOpenedEventId);
        }

        // Переопределяем поведение таймера, если нужно
        if (overrideTimerBehavior && !hasOverriddenTimer)
        {
            OverrideTimerBehavior();
        }
    }

    private void OverrideTimerBehavior()
    {
        if (cashHUD == null) return;

        hasOverriddenTimer = true;

        // Получаем настройки таймера из CashHUD через рефлексию
        float timerSeconds = GetCashHUDTimerSeconds();
        float finishedElementSeconds = GetCashHUDFinishedElementSeconds();

        if (timerSeconds <= 0f)
            timerSeconds = 40f; // Значение по умолчанию

        if (finishedElementSeconds <= 0f)
            finishedElementSeconds = 3f; // Значение по умолчанию

        // Останавливаем оригинальный таймер CashHUD
        StopOriginalCashHUDTimer();

        // Запускаем наш кастомный таймер
        customTimerCoroutine = StartCoroutine(CustomHeistTimerCoroutine(timerSeconds, finishedElementSeconds));

        if (enableDebugLogs)
            Debug.Log($"[CashHUDTaskIntegration] Overridden CashHUD timer: {timerSeconds}s");
    }

    private void StopOriginalCashHUDTimer()
    {
        if (cashHUD == null) return;

        // Останавливаем оригинальный таймер через рефлексию
        FieldInfo timerRoutineField = typeof(CashHUD).GetField("_timerRoutine", 
            BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (timerRoutineField != null)
        {
            Coroutine originalRoutine = (Coroutine)timerRoutineField.GetValue(cashHUD);
            if (originalRoutine != null)
            {
                cashHUD.StopCoroutine(originalRoutine);
                timerRoutineField.SetValue(cashHUD, null);
                
                if (enableDebugLogs)
                    Debug.Log("[CashHUDTaskIntegration] Stopped original CashHUD timer");
            }
        }
    }

    private IEnumerator CustomHeistTimerCoroutine(float timerSeconds, float finishedElementSeconds)
    {
        // Получаем UI элементы из CashHUD
        var heistTimerText = GetCashHUDField<TMPro.TMP_Text>("heistTimerText");
        var timerFinishedElement = GetCashHUDField<GameObject>("timerFinishedElement");

        // Скрываем элемент завершения
        if (timerFinishedElement != null)
            timerFinishedElement.SetActive(false);

        // Показываем таймер
        if (heistTimerText != null)
            heistTimerText.gameObject.SetActive(true);

        float remaining = Mathf.Max(0f, timerSeconds);
        while (remaining > 0f)
        {
            if (heistTimerText != null)
                heistTimerText.text = Mathf.CeilToInt(remaining).ToString();

            remaining -= Time.deltaTime;
            yield return null;
        }

        // Таймер закончился - скрываем текст таймера
        if (heistTimerText != null)
            heistTimerText.gameObject.SetActive(false);

        // АКТИВИРУЕМ СОБЫТИЕ ЗАДАЧИ
        if (useCustomTimerEndText)
        {
            ActivateCustomTaskEvent();
        }
        else if (!string.IsNullOrEmpty(timerFinishedEventId))
        {
            ActivateTaskEvent(timerFinishedEventId);
        }

        // Показываем элемент завершения
        if (timerFinishedElement != null)
        {
            timerFinishedElement.SetActive(true);
            yield return new WaitForSeconds(finishedElementSeconds);
            timerFinishedElement.SetActive(false);
        }

        customTimerCoroutine = null;

        if (enableDebugLogs)
            Debug.Log("[CashHUDTaskIntegration] Custom timer finished and task event activated!");
    }

    private void ActivateTaskEvent(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return;

        if (taskManager == null && autoFindTaskManager)
            taskManager = FindObjectOfType<TaskManager>();

        if (taskManager == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[CashHUDTaskIntegration] Cannot activate task event: TaskManager not found!");
            return;
        }

        // Проверяем существование события
        if (eventCollection != null && !eventCollection.HasEvent(eventId))
        {
            if (enableDebugLogs)
                Debug.LogError($"[CashHUDTaskIntegration] Event '{eventId}' not found in collection!");
            return;
        }

        // Находим игрока для активации
        PlayerController triggeringPlayer = GetComponent<PlayerController>();
        if (triggeringPlayer == null)
            triggeringPlayer = GetComponentInParent<PlayerController>();

        if (triggeringPlayer == null)
        {
            // Ищем любого владельца
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    triggeringPlayer = player;
                    break;
                }
            }
        }

        if (triggeringPlayer != null && taskManager.IsServer)
        {
            taskManager.TriggerEventServerRpc(eventId, triggeringPlayer.NetworkObject);
            
            if (enableDebugLogs)
                Debug.Log($"[CashHUDTaskIntegration] Activated task event: '{eventId}'");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[CashHUDTaskIntegration] Cannot activate event '{eventId}': no valid player or not server");
        }
    }

    private float GetCashHUDTimerSeconds()
    {
        return GetCashHUDFieldValue<float>("heistTimerSeconds", 40f);
    }

    private float GetCashHUDFinishedElementSeconds()
    {
        return GetCashHUDFieldValue<float>("timerFinishedElementSeconds", 3f);
    }

    private T GetCashHUDField<T>(string fieldName) where T : class
    {
        if (cashHUD == null) return null;

        FieldInfo field = typeof(CashHUD).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
        if (field != null)
            return field.GetValue(cashHUD) as T;
            
        return null;
    }

    private T GetCashHUDFieldValue<T>(string fieldName, T defaultValue) where T : struct
    {
        if (cashHUD == null) return defaultValue;

        FieldInfo field = typeof(CashHUD).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
        if (field != null)
        {
            object value = field.GetValue(cashHUD);
            if (value is T)
                return (T)value;
        }
            
        return defaultValue;
    }

    /// <summary>
    /// Ручная активация события таймера (для тестирования)
    /// </summary>
    [ContextMenu("Test Timer Finished Event")]
    public void TestTimerFinishedEvent()
    {
        ActivateTaskEvent(timerFinishedEventId);
    }

    /// <summary>
    /// Активировать кастомное событие задачи
    /// </summary>
    private void ActivateCustomTaskEvent()
    {
        if (taskManager == null && autoFindTaskManager)
            taskManager = FindObjectOfType<TaskManager>();

        if (taskManager == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[CashHUDTaskIntegration] Cannot activate custom task event: TaskManager not found!");
            return;
        }

        // Создаем временное событие с кастомными параметрами
        TaskEvent customEvent = new TaskEvent
        {
            eventId = "custom_cash_timer_end_" + Time.time, // Уникальный ID
            eventName = "Custom Cash Timer End Event",
            taskText = customTimerEndText,
            textColor = timerEndTextColor,
            fontSize = timerEndFontSize,
            canRepeat = true,
            autoHideAfter = 0f // Не скрывать автоматически
        };

        // Показываем задачу напрямую через TaskUIController на этом игроке
        TaskUIController uiController = GetComponent<TaskUIController>();
        if (uiController == null)
            uiController = GetComponentInChildren<TaskUIController>();

        if (uiController != null)
        {
            PlayerController player = uiController.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                uiController.ShowTask(customEvent);
                
                if (enableDebugLogs)
                    Debug.Log($"[CashHUDTaskIntegration] Showed custom timer end text: '{customTimerEndText}'");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[CashHUDTaskIntegration] TaskUIController not found for custom event display");
        }
    }

    /// <summary>
    /// Установить кастомный текст для окончания таймера
    /// </summary>
    public void SetCustomTimerEndText(string text, Color color, int fontSize = 0)
    {
        customTimerEndText = text;
        timerEndTextColor = color;
        if (fontSize > 0)
            timerEndFontSize = fontSize;
        useCustomTimerEndText = true;
        
        if (enableDebugLogs)
            Debug.Log($"[CashHUDTaskIntegration] Custom timer end text set: '{text}'");
    }

    /// <summary>
    /// Переключить между кастомным текстом и событиями из коллекции
    /// </summary>
    public void SetUseCustomText(bool useCustom)
    {
        useCustomTimerEndText = useCustom;
        
        if (enableDebugLogs)
            Debug.Log($"[CashHUDTaskIntegration] Use custom text: {useCustom}");
    }

    /// <summary>
    /// Установить TaskManager вручную
    /// </summary>
    public void SetTaskManager(TaskManager manager)
    {
        taskManager = manager;
    }
}