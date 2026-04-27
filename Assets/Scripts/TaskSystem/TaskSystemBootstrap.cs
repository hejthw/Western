using UnityEngine;

/// <summary>
/// Bootstrap класс для инициализации системы задач и обеспечения персистентности
/// Не является NetworkBehaviour, поэтому может использовать DontDestroyOnLoad
/// </summary>
public class TaskSystemBootstrap : MonoBehaviour
{
    [Header("Task System Setup")]
    [Tooltip("Префаб TaskManager для создания")]
    public GameObject taskManagerPrefab;
    
    [Tooltip("Автоматически создать TaskManager если его нет")]
    public bool autoCreateTaskManager = true;
    
    [Tooltip("Сохранять TaskManager между сценами")]
    public bool persistBetweenScenes = true;

    [Header("Debug")]
    [Tooltip("Показывать отладочные сообщения")]
    public bool enableDebugLogs = true;

    private static TaskSystemBootstrap instance;
    private static bool isInitialized = false;

    private void Awake()
    {
        // Singleton pattern для Bootstrap
        if (instance == null)
        {
            instance = this;
            
            if (persistBetweenScenes)
                DontDestroyOnLoad(gameObject);
            
            InitializeTaskSystem();
        }
        else if (instance != this)
        {
            if (enableDebugLogs)
                Debug.Log("[TaskSystemBootstrap] Duplicate bootstrap destroyed");
            Destroy(gameObject);
        }
    }

    private void InitializeTaskSystem()
    {
        if (isInitialized)
        {
            if (enableDebugLogs)
                Debug.Log("[TaskSystemBootstrap] Task system already initialized");
            return;
        }

        if (enableDebugLogs)
            Debug.Log("[TaskSystemBootstrap] Initializing task system...");

        // Проверяем, есть ли уже TaskManager в сцене
        TaskManager existingManager = FindObjectOfType<TaskManager>();
        
        if (existingManager == null && autoCreateTaskManager)
        {
            CreateTaskManager();
        }
        else if (existingManager != null)
        {
            if (enableDebugLogs)
                Debug.Log("[TaskSystemBootstrap] Found existing TaskManager in scene");
        }

        isInitialized = true;
        
        if (enableDebugLogs)
            Debug.Log("[TaskSystemBootstrap] Task system initialized successfully");
    }

    private void CreateTaskManager()
    {
        GameObject managerGO;
        
        if (taskManagerPrefab != null)
        {
            managerGO = Instantiate(taskManagerPrefab);
            if (enableDebugLogs)
                Debug.Log("[TaskSystemBootstrap] Created TaskManager from prefab");
        }
        else
        {
            // Создаем TaskManager с нуля
            managerGO = new GameObject("Task Manager");
            TaskManager manager = managerGO.AddComponent<TaskManager>();
            
            // Добавляем NetworkObject если его нет
            if (managerGO.GetComponent<FishNet.Object.NetworkObject>() == null)
            {
                managerGO.AddComponent<FishNet.Object.NetworkObject>();
            }
            
            if (enableDebugLogs)
                Debug.Log("[TaskSystemBootstrap] Created TaskManager from scratch");
        }
        
        // TaskManager сам решает, нужно ли ему быть персистентным через NetworkManager
        // Мы не используем DontDestroyOnLoad для NetworkBehaviour объектов
        
        if (enableDebugLogs)
            Debug.Log($"[TaskSystemBootstrap] TaskManager created: {managerGO.name}");
    }

    /// <summary>
    /// Принудительная пересоздание TaskManager (например, при смене сцены)
    /// </summary>
    public static void RecreateTaskManager()
    {
        if (instance != null && instance.autoCreateTaskManager)
        {
            TaskManager existing = FindObjectOfType<TaskManager>();
            if (existing == null)
            {
                instance.CreateTaskManager();
                
                if (instance.enableDebugLogs)
                    Debug.Log("[TaskSystemBootstrap] TaskManager recreated");
            }
        }
    }

    /// <summary>
    /// Проверить, инициализирована ли система задач
    /// </summary>
    public static bool IsSystemInitialized()
    {
        return isInitialized && TaskManager.Instance != null;
    }

    /// <summary>
    /// Получить экземпляр Bootstrap
    /// </summary>
    public static TaskSystemBootstrap GetInstance()
    {
        return instance;
    }

    private void OnApplicationQuit()
    {
        isInitialized = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Сброс статических переменных в редакторе
    /// </summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void ResetStaticVariables()
    {
        isInitialized = false;
        instance = null;
    }
#endif

    [ContextMenu("Force Initialize Task System")]
    public void ForceInitialize()
    {
        isInitialized = false;
        InitializeTaskSystem();
    }

    [ContextMenu("Debug: Show System Status")]
    public void ShowSystemStatus()
    {
        Debug.Log($"[TaskSystemBootstrap] System Status:");
        Debug.Log($"  - Bootstrap Instance: {(instance != null ? "✓" : "✗")}");
        Debug.Log($"  - System Initialized: {(isInitialized ? "✓" : "✗")}");
        Debug.Log($"  - TaskManager Instance: {(TaskManager.Instance != null ? "✓" : "✗")}");
        
        if (TaskManager.Instance != null)
        {
            var stats = TaskManager.Instance.GetStats();
            Debug.Log($"  - TaskManager Stats: {stats}");
        }
    }
}