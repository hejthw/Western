using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using System.Collections;

/// <summary>
/// Сетевой менеджер задач - синхронизирует события задач между всеми игроками
/// </summary>
public class TaskManager : NetworkBehaviour
{
    [Header("Task Configuration")]
    [Tooltip("Коллекция событий по умолчанию")]
    public TaskEventCollection defaultEventCollection;
    
    [Tooltip("Дополнительные коллекции событий")]
    public List<TaskEventCollection> additionalCollections = new List<TaskEventCollection>();
    
    [Header("UI References")]
    [Tooltip("Основной контроллер UI для задач")]
    public TaskUIController taskUIController;
    
    [Header("Settings")]
    [Tooltip("Автоматически находить UI контроллер если не указан")]
    public bool autoFindUIController = true;
    
    [Tooltip("Логировать все события задач")]
    public bool logTaskEvents = true;
    
    [Tooltip("Максимальное количество активных задач одновременно")]
    [Range(1, 10)]
    public int maxConcurrentTasks = 3;

    [Header("Persistence")]
    [Tooltip("Сохранять состояние задач между сессиями")]
    public bool persistTaskState = false;
    
    [Tooltip("Ключ для сохранения в PlayerPrefs")]
    public string saveKey = "TaskManager_State";

    // Внутренние переменные
    private readonly Dictionary<string, TaskEventCollection> allCollections = new Dictionary<string, TaskEventCollection>();
    private readonly Dictionary<string, float> triggeredEvents = new Dictionary<string, float>(); // eventId -> timestamp
    private readonly List<string> activeTaskIds = new List<string>();
    
    // События для подписки
    public System.Action<string, TaskEvent> OnTaskActivated;
    public System.Action<string> OnTaskCompleted;
    public System.Action OnTasksCleared;

    // Singleton для удобного доступа
    public static TaskManager Instance { get; private set; }

    private void Awake()
    {
        // Регистрируем себя как Instance
        if (Instance == null)
        {
            Instance = this;
            InitializeCollections();
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[TaskManager] Multiple TaskManager instances detected. Destroying duplicate: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        // Очищаем Instance если это мы
        if (Instance == this)
        {
            Instance = null;
        }
        
        if (IsServer)
            SaveTaskState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        if (autoFindUIController && taskUIController == null)
        {
            taskUIController = FindObjectOfType<TaskUIController>();
        }
        
        // Даем время UI контроллеру найти свои элементы
        if (taskUIController != null && taskUIController.autoFindUIElements)
        {
            StartCoroutine(DelayedUIControllerRefresh());
        }
        
        if (persistTaskState)
        {
            LoadTaskState();
        }
        
        if (logTaskEvents)
            Debug.Log("[TaskManager] Server started - Task system ready");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!IsServer && autoFindUIController && taskUIController == null)
        {
            taskUIController = FindObjectOfType<TaskUIController>();
        }
    }

    private void InitializeCollections()
    {
        allCollections.Clear();
        
        // Добавляем основную коллекцию
        if (defaultEventCollection != null)
        {
            allCollections[defaultEventCollection.name] = defaultEventCollection;
        }
        
        // Добавляем дополнительные коллекции
        foreach (var collection in additionalCollections)
        {
            if (collection != null && !allCollections.ContainsKey(collection.name))
            {
                allCollections[collection.name] = collection;
            }
        }
        
        if (logTaskEvents)
            Debug.Log($"[TaskManager] Initialized {allCollections.Count} event collections");
    }

    /// <summary>
    /// ServerRPC для активации события (вызывается триггерами)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TriggerEventServerRpc(string eventId, NetworkObject triggeringPlayer)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            Debug.LogWarning("[TaskManager] TriggerEvent called with empty eventId");
            return;
        }
        
        TaskEvent eventData = FindEventInCollections(eventId);
        if (eventData == null)
        {
            Debug.LogError($"[TaskManager] Event '{eventId}' not found in any collection!");
            return;
        }
        
        // Проверяем, можно ли повторно активировать событие
        if (!eventData.canRepeat && triggeredEvents.ContainsKey(eventId))
        {
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Event '{eventId}' already triggered and cannot repeat");
            return;
        }
        
        // Проверяем лимит одновременных задач
        if (activeTaskIds.Count >= maxConcurrentTasks)
        {
            if (logTaskEvents)
                Debug.LogWarning($"[TaskManager] Maximum concurrent tasks reached ({maxConcurrentTasks}). Cannot activate '{eventId}'");
            return;
        }
        
        // Записываем время активации
        triggeredEvents[eventId] = Time.time;
        activeTaskIds.Add(eventId);
        
        // Отправляем событие всем клиентам
        BroadcastTaskEventObserversRpc(eventId, triggeringPlayer);
        
        // Уведомляем подписчиков на сервере
        OnTaskActivated?.Invoke(eventId, eventData);
        
        // Автоматическое скрытие задачи
        if (eventData.autoHideAfter > 0f)
        {
            StartCoroutine(AutoHideTask(eventId, eventData.autoHideAfter));
        }
        
        if (logTaskEvents)
            Debug.Log($"[TaskManager] Event '{eventId}' activated by {(triggeringPlayer != null ? triggeringPlayer.name : "system")}");
    }

    /// <summary>
    /// ObserversRPC для синхронизации события со всеми клиентами
    /// </summary>
    [ObserversRpc]
    private void BroadcastTaskEventObserversRpc(string eventId, NetworkObject triggeringPlayer)
    {
        TaskEvent eventData = FindEventInCollections(eventId);
        if (eventData == null)
        {
            Debug.LogError($"[TaskManager] Client: Event '{eventId}' not found!");
            return;
        }
        
        // Обновляем UI
        if (taskUIController != null)
        {
            taskUIController.ShowTask(eventData);
        }
        else if (logTaskEvents)
        {
            Debug.LogWarning("[TaskManager] TaskUIController not found! Task UI will not be updated.");
        }
        
        // Воспроизводим звук
        if (eventData.activationSound != null)
        {
            AudioSource.PlayClipAtPoint(eventData.activationSound, Camera.main.transform.position, eventData.soundVolume);
        }
        
        // Уведомляем подписчиков
        OnTaskActivated?.Invoke(eventId, eventData);
        
        if (logTaskEvents)
            Debug.Log($"[TaskManager] Client received task event: '{eventId}'");
    }

    /// <summary>
    /// Завершить задачу вручную
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CompleteTaskServerRpc(string eventId)
    {
        if (activeTaskIds.Contains(eventId))
        {
            activeTaskIds.Remove(eventId);
            CompleteTaskObserversRpc(eventId);
            
            OnTaskCompleted?.Invoke(eventId);
            
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Task '{eventId}' completed manually");
        }
    }

    [ObserversRpc]
    private void CompleteTaskObserversRpc(string eventId)
    {
        if (taskUIController != null)
        {
            taskUIController.HideTask(eventId);
        }
        
        OnTaskCompleted?.Invoke(eventId);
    }

    /// <summary>
    /// Очистить все активные задачи
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ClearAllTasksServerRpc()
    {
        activeTaskIds.Clear();
        ClearAllTasksObserversRpc();
        
        OnTasksCleared?.Invoke();
        
        if (logTaskEvents)
            Debug.Log("[TaskManager] All tasks cleared");
    }

    [ObserversRpc]
    private void ClearAllTasksObserversRpc()
    {
        if (taskUIController != null)
        {
            taskUIController.ClearAllTasks();
        }
        
        OnTasksCleared?.Invoke();
    }

    private TaskEvent FindEventInCollections(string eventId)
    {
        foreach (var collection in allCollections.Values)
        {
            TaskEvent eventData = collection.GetEvent(eventId);
            if (eventData != null)
                return eventData;
        }
        return null;
    }

    private IEnumerator AutoHideTask(string eventId, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (activeTaskIds.Contains(eventId))
        {
            CompleteTaskServerRpc(eventId);
        }
    }

    /// <summary>
    /// Добавить коллекцию событий во время выполнения
    /// </summary>
    public void AddEventCollection(TaskEventCollection collection)
    {
        if (collection != null && !allCollections.ContainsKey(collection.name))
        {
            allCollections[collection.name] = collection;
            
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Added event collection: {collection.name}");
        }
    }

    /// <summary>
    /// Получить все активные задачи
    /// </summary>
    public List<string> GetActiveTasks()
    {
        return new List<string>(activeTaskIds);
    }

    /// <summary>
    /// Проверить, активна ли задача
    /// </summary>
    public bool IsTaskActive(string eventId)
    {
        return activeTaskIds.Contains(eventId);
    }

    /// <summary>
    /// Получить время активации события
    /// </summary>
    public float GetEventTriggerTime(string eventId)
    {
        return triggeredEvents.TryGetValue(eventId, out float time) ? time : -1f;
    }

    /// <summary>
    /// Сохранить состояние задач
    /// </summary>
    private void SaveTaskState()
    {
        if (!persistTaskState) return;
        
        TaskManagerSaveData saveData = new TaskManagerSaveData
        {
            triggeredEvents = triggeredEvents,
            activeTaskIds = activeTaskIds
        };
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        
        if (logTaskEvents)
            Debug.Log("[TaskManager] Task state saved");
    }

    /// <summary>
    /// Загрузить состояние задач
    /// </summary>
    private void LoadTaskState()
    {
        if (!persistTaskState) return;
        
        string json = PlayerPrefs.GetString(saveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                TaskManagerSaveData saveData = JsonUtility.FromJson<TaskManagerSaveData>(json);
                
                // Восстанавливаем состояние (только на сервере)
                if (IsServer)
                {
                    foreach (var kvp in saveData.triggeredEvents)
                        triggeredEvents[kvp.Key] = kvp.Value;
                    
                    foreach (string taskId in saveData.activeTaskIds)
                        activeTaskIds.Add(taskId);
                }
                
                if (logTaskEvents)
                    Debug.Log($"[TaskManager] Task state loaded: {triggeredEvents.Count} events, {activeTaskIds.Count} active tasks");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TaskManager] Failed to load task state: {e.Message}");
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsServer)
            SaveTaskState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsServer)
            SaveTaskState();
    }


    /// <summary>
    /// Получить статистику менеджера задач
    /// </summary>
    public TaskManagerStats GetStats()
    {
        return new TaskManagerStats
        {
            totalCollections = allCollections.Count,
            totalEvents = GetTotalEventCount(),
            triggeredEvents = triggeredEvents.Count,
            activeTasks = activeTaskIds.Count,
            maxConcurrentTasks = maxConcurrentTasks
        };
    }

    private int GetTotalEventCount()
    {
        int count = 0;
        foreach (var collection in allCollections.Values)
        {
            if (collection != null)
                count += collection.events.Count;
        }
        return count;
    }

    [ContextMenu("Debug: Show Active Tasks")]
    public void DebugShowActiveTasks()
    {
        Debug.Log($"[TaskManager] Active tasks ({activeTaskIds.Count}):");
        foreach (string taskId in activeTaskIds)
        {
            float triggerTime = GetEventTriggerTime(taskId);
            Debug.Log($"  - {taskId} (triggered at: {triggerTime})");
        }
    }

    [ContextMenu("Debug: Clear All Tasks")]
    public void DebugClearAllTasks()
    {
        if (IsServer)
            ClearAllTasksServerRpc();
    }

    /// <summary>
    /// Корутина для отложенного обновления UI контроллера
    /// </summary>
    private System.Collections.IEnumerator DelayedUIControllerRefresh()
    {
        yield return new WaitForSeconds(2f); // Ждем, пока игроки точно спавнятся
        
        if (taskUIController != null)
        {
            taskUIController.RefreshUIElements();
            
            if (logTaskEvents)
                Debug.Log("[TaskManager] UI Controller refreshed after player spawn");
        }
    }
}

/// <summary>
/// Данные для сохранения состояния TaskManager
/// </summary>
[System.Serializable]
public class TaskManagerSaveData
{
    public Dictionary<string, float> triggeredEvents = new Dictionary<string, float>();
    public List<string> activeTaskIds = new List<string>();
}

/// <summary>
/// Статистика TaskManager
/// </summary>
[System.Serializable]
public class TaskManagerStats
{
    public int totalCollections;
    public int totalEvents;
    public int triggeredEvents;
    public int activeTasks;
    public int maxConcurrentTasks;
    
    public override string ToString()
    {
        return $"Collections: {totalCollections}, Events: {totalEvents}, Triggered: {triggeredEvents}, Active: {activeTasks}/{maxConcurrentTasks}";
    }
}