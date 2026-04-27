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

    // Singleton pattern
    public static TaskManager Instance { get; private set; }

    #region Unity & FishNet Lifecycle

    private void Awake()
    {
        // Настройка singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void OnStartServer()
    {
        if (logTaskEvents)
            Debug.Log("[TaskManager] Started as server");
        
        InitializeCollections();
        LoadPersistentState();
        
        // Поиск UI контроллера
        if (autoFindUIController)
        {
            StartCoroutine(DelayedUIControllerSearch());
        }
    }

    public override void OnStartClient()
    {
        if (logTaskEvents)
            Debug.Log("[TaskManager] Started as client");
        
        InitializeCollections();
        
        // Поиск UI контроллера
        if (autoFindUIController)
        {
            StartCoroutine(DelayedUIControllerSearch());
        }
    }

    public override void OnStopServer()
    {
        SavePersistentState();
        
        if (logTaskEvents)
            Debug.Log("[TaskManager] Stopped server");
    }

    #endregion

    #region Initialization

    private void InitializeCollections()
    {
        allCollections.Clear();
        
        // Добавляем коллекцию по умолчанию
        if (defaultEventCollection != null)
        {
            allCollections[defaultEventCollection.collectionName] = defaultEventCollection;
        }
        
        // Добавляем дополнительные коллекции
        foreach (var collection in additionalCollections)
        {
            if (collection != null)
            {
                allCollections[collection.collectionName] = collection;
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
        if (logTaskEvents)
            Debug.Log($"[TaskManager] Server received TriggerEventServerRpc: eventId='{eventId}', triggeringPlayer={triggeringPlayer?.name}, IsServer={IsServer}");
            
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
        
        // Проверяем, не активно ли уже это событие
        if (activeTaskIds.Contains(eventId))
        {
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Event '{eventId}' is already active");
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
        if (logTaskEvents)
            Debug.Log($"[TaskManager] Client received BroadcastTaskEventObserversRpc: eventId='{eventId}', triggeringPlayer={triggeringPlayer?.name}");
            
        TaskEvent eventData = FindEventInCollections(eventId);
        if (eventData == null)
        {
            Debug.LogError($"[TaskManager] Client: Event '{eventId}' not found!");
            return;
        }
        
        TaskUIController localUIController = FindLocalOwnerTaskUIController();
        if (localUIController != null)
            localUIController.ShowTask(eventData);
        
        // Уведомляем подписчиков на клиенте
        OnTaskActivated?.Invoke(eventId, eventData);
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
                Debug.Log($"[TaskManager] Task '{eventId}' completed");
        }
    }

    [ObserversRpc]
    private void CompleteTaskObserversRpc(string eventId)
    {
        TaskUIController localUIController = FindLocalOwnerTaskUIController();
        if (localUIController != null)
            localUIController.HideTask(eventId);
        
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
        TaskUIController localUIController = FindLocalOwnerTaskUIController();
        if (localUIController != null)
            localUIController.ClearAllTasks();
        
        OnTasksCleared?.Invoke();
    }

    #endregion

    #region Task Management

    /// <summary>
    /// Корутина для автоматического скрытия задачи
    /// </summary>
    private System.Collections.IEnumerator AutoHideTask(string eventId, float delay)
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
        if (collection != null)
        {
            allCollections[collection.collectionName] = collection;
            
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Added collection: {collection.collectionName}");
        }
    }

    /// <summary>
    /// Удалить коллекцию событий
    /// </summary>
    public void RemoveEventCollection(string collectionName)
    {
        if (allCollections.ContainsKey(collectionName))
        {
            allCollections.Remove(collectionName);
            
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Removed collection: {collectionName}");
        }
    }

    /// <summary>
    /// Найти событие во всех коллекциях
    /// </summary>
    private TaskEvent FindEventInCollections(string eventId)
    {
        foreach (var collection in allCollections.Values)
        {
            TaskEvent foundEvent = collection.GetEvent(eventId);
            if (foundEvent != null)
            {
                return foundEvent;
            }
        }
        return null;
    }

    /// <summary>
    /// Получить все активные задачи
    /// </summary>
    public List<string> GetActiveTaskIds()
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
    /// Получить время активации задачи
    /// </summary>
    public float GetTaskActivationTime(string eventId)
    {
        return triggeredEvents.TryGetValue(eventId, out float time) ? time : -1f;
    }

    #endregion

    #region Persistence

    private void SavePersistentState()
    {
        if (!persistTaskState) return;
        
        // Простое сохранение активных задач
        string activeTasksJson = string.Join(",", activeTaskIds);
        PlayerPrefs.SetString(saveKey, activeTasksJson);
        
        if (logTaskEvents)
            Debug.Log($"[TaskManager] Saved {activeTaskIds.Count} active tasks");
    }

    private void LoadPersistentState()
    {
        if (!persistTaskState) return;
        
        string savedTasks = PlayerPrefs.GetString(saveKey, "");
        if (!string.IsNullOrEmpty(savedTasks))
        {
            activeTaskIds.Clear();
            activeTaskIds.AddRange(savedTasks.Split(','));
            
            if (logTaskEvents)
                Debug.Log($"[TaskManager] Loaded {activeTaskIds.Count} active tasks");
        }
    }

    #endregion

    #region UI Management

    private TaskUIController FindLocalOwnerTaskUIController()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            if (player != null && player.IsOwner)
            {
                TaskUIController ownerController = player.GetComponentInChildren<TaskUIController>(true);
                if (ownerController != null)
                    return ownerController;
            }
        }

        if (taskUIController != null)
            return taskUIController;

        return FindObjectOfType<TaskUIController>();
    }

    /// <summary>
    /// Корутина для отложенного обновления UI контроллера
    /// </summary>
    private System.Collections.IEnumerator DelayedUIControllerSearch()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (taskUIController == null)
        {
            taskUIController = FindObjectOfType<TaskUIController>();
            
            if (taskUIController != null && logTaskEvents)
            {
                Debug.Log($"[TaskManager] Found TaskUIController: {taskUIController.name}");
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Очистить все задачи (публичный метод)
    /// </summary>
    [ContextMenu("Clear All Tasks")]
    public void ClearAllTasks()
    {
        if (IsServer)
            ClearAllTasksServerRpc();
    }

    /// <summary>
    /// Получить информацию о системе (для отладки)
    /// </summary>
    public string GetSystemInfo()
    {
        return $"TaskManager Info:\n" +
               $"- Role: {(IsServer ? "Server" : "Client")}\n" +
               $"- Collections: {allCollections.Count}\n" +
               $"- Active Tasks: {activeTaskIds.Count}\n" +
               $"- UI Controller: {(taskUIController != null ? taskUIController.name : "null")}";
    }

    #endregion
}