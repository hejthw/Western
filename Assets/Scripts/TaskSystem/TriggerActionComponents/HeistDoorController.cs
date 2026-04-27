using UnityEngine;

/// <summary>
/// Контроллер для управления HeistDoor через триггеры
/// </summary>
public class HeistDoorController : MonoBehaviour
{
    [Header("Door Control")]
    [Tooltip("HeistDoor для управления")]
    public HeistDoor targetDoor;
    
    [Tooltip("Автоматически найти HeistDoor на этом объекте")]
    public bool autoFindDoor = true;

    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool enableDebugLogs = false;

    private void Awake()
    {
        if (autoFindDoor && targetDoor == null)
        {
            targetDoor = GetComponent<HeistDoor>();
            
            if (targetDoor == null)
                targetDoor = GetComponentInChildren<HeistDoor>();
        }
    }

    /// <summary>
    /// Открыть дверь (вызывается из UnityEvent)
    /// </summary>
    public void OpenDoor()
    {
        if (targetDoor == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[HeistDoorController] No target door assigned!");
            return;
        }

        if (targetDoor.IsOpened())
        {
            if (enableDebugLogs)
                Debug.Log("[HeistDoorController] Door is already open");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[HeistDoorController] Opening door: {targetDoor.gameObject.name}");

        targetDoor.ServerToggleDoor();
    }

    /// <summary>
    /// Переключить состояние двери
    /// </summary>
    public void ToggleDoor()
    {
        if (targetDoor == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[HeistDoorController] No target door assigned!");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[HeistDoorController] Toggling door: {targetDoor.gameObject.name}");

        targetDoor.ServerToggleDoor();
    }

    /// <summary>
    /// Проверить, открыта ли дверь
    /// </summary>
    public bool IsDoorOpen()
    {
        return targetDoor != null && targetDoor.IsOpened();
    }

    /// <summary>
    /// Установить целевую дверь
    /// </summary>
    public void SetTargetDoor(HeistDoor door)
    {
        targetDoor = door;
        
        if (enableDebugLogs)
            Debug.Log($"[HeistDoorController] Target door set to: {(door != null ? door.gameObject.name : "null")}");
    }
}