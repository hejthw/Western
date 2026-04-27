using UnityEngine;
using FishNet.Object;

/// <summary>
/// Отладчик системы задач для диагностики проблем с сетевой синхронизацией
/// </summary>
public class TaskSystemDebugger : NetworkBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Показывать информацию на экране")]
    public bool showOnScreenDebug = true;
    
    [Tooltip("Размер текста отладки")]
    public int debugTextSize = 12;
    
    [Tooltip("Автоматически обновлять информацию")]
    public bool autoUpdate = true;
    
    [Tooltip("Интервал обновления (секунды)")]
    public float updateInterval = 1f;

    private string debugInfo = "";
    private float lastUpdateTime;

    private void Update()
    {
        if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateDebugInfo()
    {
        debugInfo = GetSystemStatus();
    }

    private string GetSystemStatus()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("=== TASK SYSTEM DEBUG ===");
        sb.AppendLine($"Role: {(IsServer ? "SERVER" : "CLIENT")}");
        sb.AppendLine($"IsOwner: {IsOwner}");
        
        // Информация о TaskManager
        TaskManager[] taskManagers = FindObjectsOfType<TaskManager>();
        sb.AppendLine($"TaskManagers found: {taskManagers.Length}");
        
        for (int i = 0; i < taskManagers.Length; i++)
        {
            TaskManager tm = taskManagers[i];
            PlayerController player = tm.GetComponent<PlayerController>();
            sb.AppendLine($"  [{i}] {tm.name} - Server:{tm.IsServer} - Player:{(player ? player.name : "Scene")} - Owner:{(player ? player.IsOwner.ToString() : "N/A")}");
        }
        
        // Информация о TaskUIController
        TaskUIController[] uiControllers = FindObjectsOfType<TaskUIController>();
        sb.AppendLine($"TaskUIControllers found: {uiControllers.Length}");
        
        for (int i = 0; i < uiControllers.Length; i++)
        {
            TaskUIController ui = uiControllers[i];
            PlayerController player = ui.GetComponent<PlayerController>();
            sb.AppendLine($"  [{i}] {ui.name} - Player:{(player ? player.name : "Scene")} - Owner:{(player ? player.IsOwner.ToString() : "N/A")} - ActiveTasks:{ui.GetActiveTaskCount()}");
        }
        
        // Информация о TaskEventTrigger
        TaskEventTrigger[] triggers = FindObjectsOfType<TaskEventTrigger>();
        sb.AppendLine($"TaskEventTriggers found: {triggers.Length}");
        
        for (int i = 0; i < triggers.Length; i++)
        {
            TaskEventTrigger trigger = triggers[i];
            sb.AppendLine($"  [{i}] {trigger.name} - Event:{trigger.eventToTrigger} - Status:{trigger.GetStatusInfo()}");
        }
        
        // Информация о HeistTimerEventHandler
        HeistTimerEventHandler[] timers = FindObjectsOfType<HeistTimerEventHandler>();
        sb.AppendLine($"HeistTimerEventHandlers found: {timers.Length}");
        
        for (int i = 0; i < timers.Length; i++)
        {
            HeistTimerEventHandler timer = timers[i];
            PlayerController player = timer.GetComponent<PlayerController>();
            sb.AppendLine($"  [{i}] {timer.name} - Player:{(player ? player.name : "Scene")} - Running:{timer.IsTimerRunning()}");
        }
        
        return sb.ToString();
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = debugTextSize;
        style.normal.textColor = Color.white;
        
        // Фон для лучшей читаемости
        GUI.Box(new Rect(10, 10, 400, 300), "");
        GUI.Label(new Rect(15, 15, 390, 290), debugInfo, style);
    }

    [ContextMenu("Update Debug Info")]
    public void ManualUpdateDebugInfo()
    {
        UpdateDebugInfo();
        Debug.Log(debugInfo);
    }

    [ContextMenu("Test Task Event")]
    public void TestTaskEvent()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TaskSystemDebugger] Can only test events from server");
            return;
        }
        
        TaskManager[] taskManagers = FindObjectsOfType<TaskManager>();
        TaskManager serverTaskManager = null;
        
        foreach (var tm in taskManagers)
        {
            if (tm.IsServer)
            {
                serverTaskManager = tm;
                break;
            }
        }
        
        if (serverTaskManager != null)
        {
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            PlayerController testPlayer = null;
            
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    testPlayer = player;
                    break;
                }
            }
            
            if (testPlayer != null)
            {
                serverTaskManager.TriggerEventServerRpc("test_event", testPlayer.NetworkObject);
                Debug.Log("[TaskSystemDebugger] Test event triggered");
            }
            else
            {
                Debug.LogWarning("[TaskSystemDebugger] No owner player found for test");
            }
        }
        else
        {
            Debug.LogWarning("[TaskSystemDebugger] No server TaskManager found");
        }
    }
}