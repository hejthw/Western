using UnityEngine;
using FishNet.Object;

/// <summary>
/// Bootstrap компонент для системы задач
/// Управляет жизненным циклом TaskManager и обеспечивает его персистентность
/// </summary>
public class TaskSystemBootstrap : MonoBehaviour
{
    [Header("Task Manager Settings")]
    [Tooltip("Префаб TaskManager для создания")]
    public GameObject taskManagerPrefab;
    
    [Tooltip("Автоматически создавать TaskManager если не найден")]
    public bool autoCreateTaskManager = true;
    
    [Tooltip("Сохранять между сценами")]
    public bool persistBetweenScenes = true;
    
    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool enableDebugLogs = false;

    private TaskManager taskManagerInstance;
    private static TaskSystemBootstrap instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
        
        InitializeTaskManager();
    }

    private void InitializeTaskManager()
    {
        // Ищем существующий TaskManager
        taskManagerInstance = FindObjectOfType<TaskManager>();
        
        if (taskManagerInstance == null && autoCreateTaskManager)
        {
            CreateTaskManager();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TaskSystemBootstrap] TaskManager initialized: {(taskManagerInstance != null ? taskManagerInstance.name : "null")}");
        }
    }

    private void CreateTaskManager()
    {
        GameObject managerObject;
        
        if (taskManagerPrefab != null)
        {
            managerObject = Instantiate(taskManagerPrefab);
        }
        else
        {
            // Создаем простой объект с TaskManager
            managerObject = new GameObject("TaskManager");
            managerObject.AddComponent<TaskManager>();
            
            // Добавляем NetworkObject если его нет
            if (managerObject.GetComponent<NetworkObject>() == null)
            {
                managerObject.AddComponent<NetworkObject>();
            }
        }
        
        taskManagerInstance = managerObject.GetComponent<TaskManager>();
        
        if (persistBetweenScenes)
        {
            // НЕ вызываем DontDestroyOnLoad для NetworkBehaviour!
            // Это управляется через Bootstrap
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TaskSystemBootstrap] Created TaskManager: {managerObject.name}");
        }
    }

    /// <summary>
    /// Получить текущий экземпляр TaskManager
    /// </summary>
    public TaskManager GetTaskManager()
    {
        if (taskManagerInstance == null)
        {
            taskManagerInstance = FindObjectOfType<TaskManager>();
        }
        
        return taskManagerInstance;
    }

    /// <summary>
    /// Пересоздать TaskManager (например, при смене сцены)
    /// </summary>
    [ContextMenu("Recreate Task Manager")]
    public void RecreateTaskManager()
    {
        if (taskManagerInstance != null)
        {
            if (enableDebugLogs)
                Debug.Log("[TaskSystemBootstrap] Destroying old TaskManager");
                
            Destroy(taskManagerInstance.gameObject);
            taskManagerInstance = null;
        }
        
        CreateTaskManager();
    }

    /// <summary>
    /// Проверить статус системы (для отладки)
    /// </summary>
    [ContextMenu("Debug System Status")]
    public void DebugSystemStatus()
    {
        Debug.Log("=== Task System Status ===");
        Debug.Log($"Bootstrap: {(instance != null ? "Active" : "Missing")}");
        Debug.Log($"TaskManager: {(taskManagerInstance != null ? taskManagerInstance.name : "null")}");
        
        if (taskManagerInstance != null)
        {
            Debug.Log(taskManagerInstance.GetSystemInfo());
        }
        
        TaskUIController[] uiControllers = FindObjectsOfType<TaskUIController>();
        Debug.Log($"TaskUIControllers: {uiControllers.Length}");
        
        TaskEventTrigger[] triggers = FindObjectsOfType<TaskEventTrigger>();
        Debug.Log($"TaskEventTriggers: {triggers.Length}");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Получить глобальный экземпляр Bootstrap
    /// </summary>
    public static TaskSystemBootstrap Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TaskSystemBootstrap>();
            }
            return instance;
        }
    }
}