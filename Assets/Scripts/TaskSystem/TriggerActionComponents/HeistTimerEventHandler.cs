using UnityEngine;
using System.Collections;

/// <summary>
/// Обработчик событий таймера ограбления для интеграции с системой задач
/// </summary>
public class HeistTimerEventHandler : MonoBehaviour
{
    [Header("Task System Integration")]
    [Tooltip("ID события задачи, которое активируется при окончании таймера")]
    public string taskEventIdOnTimerEnd = "heist_timer_finished";
    
    [Tooltip("Коллекция событий для активации")]
    public TaskEventCollection eventCollection;
    
    [Tooltip("Автоматически найти TaskManager в сцене")]
    public bool autoFindTaskManager = true;

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

    [Header("Timer Settings")]
    [Tooltip("Длительность таймера (секунды). Если 0, использует настройку из CashHUD")]
    public float customTimerDuration = 0f;
    
    [Tooltip("Показывать обратный отсчёт в консоли")]
    public bool showCountdownLogs = false;

    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool enableDebugLogs = true;

    private TaskManager taskManager;
    private bool timerStarted = false;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        // Подписываемся на событие открытия двери
        HeistDoor.OpenedByLocalPlayer += OnHeistDoorOpened;
    }

    private void Start()
    {
        if (autoFindTaskManager)
        {
            taskManager = FindObjectOfType<TaskManager>();
            
            if (taskManager == null && enableDebugLogs)
                Debug.LogWarning("[HeistTimerEventHandler] TaskManager not found in scene!");
        }
    }

    private void OnDestroy()
    {
        HeistDoor.OpenedByLocalPlayer -= OnHeistDoorOpened;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private void OnHeistDoorOpened()
    {
        if (timerStarted)
        {
            if (enableDebugLogs)
                Debug.Log("[HeistTimerEventHandler] Timer already started, ignoring");
            return;
        }

        // Запускаем таймер только на сервере или у владельца
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Если на игроке, запускаем только у владельца
            if (playerController.IsOwner)
            {
                StartHeistTimer();
            }
        }
        else
        {
            // Если в сцене, запускаем везде (но активация будет только на сервере)
            StartHeistTimer();
        }
    }

    /// <summary>
    /// Запустить таймер ограбления
    /// </summary>
    public void StartHeistTimer()
    {
        if (timerStarted)
        {
            if (enableDebugLogs)
                Debug.Log("[HeistTimerEventHandler] Timer already running");
            return;
        }

        // Определяем длительность таймера
        float timerDuration = customTimerDuration;
        
        if (timerDuration <= 0f)
        {
            // Пытаемся найти CashHUD для получения стандартной длительности
            CashHUD cashHUD = FindObjectOfType<CashHUD>();
            if (cashHUD != null)
            {
                // Используем рефлексию для доступа к приватному полю (или можно сделать его публичным)
                var field = typeof(CashHUD).GetField("heistTimerSeconds", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    timerDuration = (float)field.GetValue(cashHUD);
                }
            }
            
            // Если всё ещё 0, используем значение по умолчанию
            if (timerDuration <= 0f)
                timerDuration = 40f;
        }

        timerStarted = true;
        timerCoroutine = StartCoroutine(HeistTimerCoroutine(timerDuration));
        
        if (enableDebugLogs)
            Debug.Log($"[HeistTimerEventHandler] Started heist timer: {timerDuration} seconds");
    }

    /// <summary>
    /// Остановить таймер принудительно
    /// </summary>
    public void StopHeistTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            timerStarted = false;
            
            if (enableDebugLogs)
                Debug.Log("[HeistTimerEventHandler] Heist timer stopped");
        }
    }

    /// <summary>
    /// Сбросить состояние таймера (для повторного использования)
    /// </summary>
    public void ResetTimer()
    {
        StopHeistTimer();
        timerStarted = false;
        
        if (enableDebugLogs)
            Debug.Log("[HeistTimerEventHandler] Timer reset");
    }

    private IEnumerator HeistTimerCoroutine(float duration)
    {
        float remaining = duration;
        
        while (remaining > 0f)
        {
            if (showCountdownLogs && Mathf.CeilToInt(remaining) % 10 == 0) // Каждые 10 секунд
            {
                Debug.Log($"[HeistTimerEventHandler] Timer: {Mathf.CeilToInt(remaining)} seconds remaining");
            }
            
            remaining -= Time.deltaTime;
            yield return null;
        }

        // Таймер закончился
        OnTimerFinished();
        
        timerCoroutine = null;
        timerStarted = false;
    }

    private void OnTimerFinished()
    {
        if (enableDebugLogs)
            Debug.Log("[HeistTimerEventHandler] Heist timer finished!");

        // Активируем событие задачи
        if (useCustomTimerEndText)
        {
            ActivateCustomTaskEvent();
        }
        else if (!string.IsNullOrEmpty(taskEventIdOnTimerEnd))
        {
            ActivateTaskEvent(taskEventIdOnTimerEnd);
        }
        
        // Можно добавить дополнительные действия здесь
        OnTimerFinishedEvent();
    }

    private void ActivateTaskEvent(string eventId)
    {
        if (taskManager == null)
        {
            if (autoFindTaskManager)
                taskManager = FindObjectOfType<TaskManager>();
                
            if (taskManager == null)
            {
                if (enableDebugLogs)
                    Debug.LogError("[HeistTimerEventHandler] Cannot activate task event: TaskManager not found!");
                return;
            }
        }

        // Проверяем, что событие существует в коллекции
        if (eventCollection != null && !eventCollection.HasEvent(eventId))
        {
            if (enableDebugLogs)
                Debug.LogError($"[HeistTimerEventHandler] Event '{eventId}' not found in collection!");
            return;
        }

        // Находим любого игрока для активации (системное событие)
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        PlayerController triggeringPlayer = null;
        
        foreach (var player in players)
        {
            if (player.IsOwner) // Предпочитаем локального игрока
            {
                triggeringPlayer = player;
                break;
            }
        }
        
        if (triggeringPlayer == null && players.Length > 0)
            triggeringPlayer = players[0]; // Любого, если нет локального

        // Находим TaskManager на сервере
        TaskManager[] taskManagers = FindObjectsOfType<TaskManager>();
        TaskManager serverTaskManager = null;
        
        foreach (var manager in taskManagers)
        {
            if (manager.IsServer)
            {
                serverTaskManager = manager;
                break;
            }
        }
        
        if (triggeringPlayer != null && serverTaskManager != null)
        {
            serverTaskManager.TriggerEventServerRpc(eventId, triggeringPlayer.NetworkObject);
            
            if (enableDebugLogs)
                Debug.Log($"[HeistTimerEventHandler] Activated task event: '{eventId}' on server TaskManager");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[HeistTimerEventHandler] Cannot activate event: triggeringPlayer={triggeringPlayer != null}, serverTaskManager={serverTaskManager != null}");
        }
    }

    /// <summary>
    /// Событие окончания таймера (для внешних подписчиков)
    /// </summary>
    public System.Action OnTimerFinishedEvent = delegate { };

    /// <summary>
    /// Ручная активация события таймера (для тестирования)
    /// </summary>
    [ContextMenu("Trigger Timer Finished Event")]
    public void ManuallyTriggerTimerFinished()
    {
        OnTimerFinished();
    }

    /// <summary>
    /// Установить TaskManager вручную
    /// </summary>
    public void SetTaskManager(TaskManager manager)
    {
        taskManager = manager;
        
        if (enableDebugLogs)
            Debug.Log($"[HeistTimerEventHandler] TaskManager set: {(manager != null ? manager.name : "null")}");
    }

    /// <summary>
    /// Получить оставшееся время таймера
    /// </summary>
    public bool IsTimerRunning() => timerStarted && timerCoroutine != null;

    /// <summary>
    /// Активировать кастомное событие задачи
    /// </summary>
    private void ActivateCustomTaskEvent()
    {
        if (taskManager == null)
        {
            if (autoFindTaskManager)
                taskManager = FindObjectOfType<TaskManager>();
                
            if (taskManager == null)
            {
                if (enableDebugLogs)
                    Debug.LogError("[HeistTimerEventHandler] Cannot activate custom task event: TaskManager not found!");
                return;
            }
        }

        // Создаем временное событие с кастомными параметрами
        TaskEvent customEvent = new TaskEvent
        {
            eventId = "custom_timer_end_" + Time.time, // Уникальный ID
            eventName = "Custom Timer End Event",
            taskText = customTimerEndText,
            textColor = timerEndTextColor,
            fontSize = timerEndFontSize,
            canRepeat = true,
            autoHideAfter = 0f // Не скрывать автоматически
        };

        // Если HeistTimerEventHandler на игроке, показываем только ему
        PlayerController myPlayer = GetComponent<PlayerController>();
        if (myPlayer != null && myPlayer.IsOwner)
        {
            TaskUIController myUIController = GetComponent<TaskUIController>();
            if (myUIController == null)
                myUIController = GetComponentInChildren<TaskUIController>();
                
            if (myUIController != null)
            {
                myUIController.ShowTask(customEvent);
                
                if (enableDebugLogs)
                    Debug.Log($"[HeistTimerEventHandler] Showed custom timer end text to local player");
            }
        }
        else if (myPlayer == null)
        {
            // Если в сцене, показываем всем владельцам
            TaskUIController[] uiControllers = FindObjectsOfType<TaskUIController>();
            
            foreach (var uiController in uiControllers)
            {
                PlayerController player = uiController.GetComponent<PlayerController>();
                if (player != null && player.IsOwner)
                {
                    uiController.ShowTask(customEvent);
                    
                    if (enableDebugLogs)
                        Debug.Log($"[HeistTimerEventHandler] Showed custom timer end text to player: {player.name}");
                }
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[HeistTimerEventHandler] Activated custom task event with text: '{customTimerEndText}'");
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
            Debug.Log($"[HeistTimerEventHandler] Custom timer end text set: '{text}'");
    }

    /// <summary>
    /// Установить ID события для окончания таймера
    /// </summary>
    public void SetTimerEndEventId(string eventId)
    {
        taskEventIdOnTimerEnd = eventId;
        useCustomTimerEndText = false; // Переключаемся на использование событий из коллекции
        
        if (enableDebugLogs)
            Debug.Log($"[HeistTimerEventHandler] Timer end event ID set to: '{eventId}'");
    }

    /// <summary>
    /// Переключить между кастомным текстом и событиями из коллекции
    /// </summary>
    public void SetUseCustomText(bool useCustom)
    {
        useCustomTimerEndText = useCustom;
        
        if (enableDebugLogs)
            Debug.Log($"[HeistTimerEventHandler] Use custom text: {useCustom}");
    }
}