using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject содержащий коллекцию событий задач
/// </summary>
[CreateAssetMenu(fileName = "TaskEventCollection", menuName = "TaskSystem/Task Event Collection")]
public class TaskEventCollection : ScriptableObject
{
    [Header("Collection Info")]
    [Tooltip("Название коллекции для удобства")]
    public string collectionName;
    
    [TextArea(2, 4)]
    [Tooltip("Описание коллекции")]
    public string description;
    
    [Header("Events")]
    [Tooltip("Список всех событий в коллекции")]
    public List<TaskEvent> events = new List<TaskEvent>();
    
    [Header("Default Settings")]
    [Tooltip("Настройки UI по умолчанию")]
    public TaskUISettings defaultUISettings = new TaskUISettings();

    /// <summary>
    /// Получить событие по ID
    /// </summary>
    public TaskEvent GetEvent(string eventId)
    {
        return events.FirstOrDefault(e => e.eventId == eventId);
    }

    /// <summary>
    /// Проверить, существует ли событие с таким ID
    /// </summary>
    public bool HasEvent(string eventId)
    {
        return events.Any(e => e.eventId == eventId);
    }

    /// <summary>
    /// Добавить новое событие
    /// </summary>
    public void AddEvent(TaskEvent taskEvent)
    {
        if (taskEvent == null || !taskEvent.IsValid()) return;
        
        // Проверяем уникальность ID
        if (HasEvent(taskEvent.eventId))
        {
            Debug.LogWarning($"[TaskEventCollection] Event with ID '{taskEvent.eventId}' already exists!");
            return;
        }
        
        events.Add(taskEvent);
    }

    /// <summary>
    /// Удалить событие по ID
    /// </summary>
    public bool RemoveEvent(string eventId)
    {
        TaskEvent eventToRemove = GetEvent(eventId);
        if (eventToRemove != null)
        {
            events.Remove(eventToRemove);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Получить все ID событий
    /// </summary>
    public string[] GetAllEventIds()
    {
        return events.Where(e => e.IsValid()).Select(e => e.eventId).ToArray();
    }

    /// <summary>
    /// Валидация коллекции
    /// </summary>
    public ValidationResult Validate()
    {
        ValidationResult result = new ValidationResult();
        
        // Проверяем дубликаты ID
        var duplicateIds = events
            .Where(e => e.IsValid())
            .GroupBy(e => e.eventId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateIds.Count > 0)
        {
            result.AddError($"Duplicate event IDs found: {string.Join(", ", duplicateIds)}");
        }
        
        // Проверяем пустые события
        var invalidEvents = events.Where(e => !e.IsValid()).ToList();
        if (invalidEvents.Count > 0)
        {
            result.AddWarning($"{invalidEvents.Count} invalid events found (empty ID or text)");
        }
        
        return result;
    }

    /// <summary>
    /// Очистить невалидные события
    /// </summary>
    [ContextMenu("Clean Invalid Events")]
    public void CleanInvalidEvents()
    {
        events.RemoveAll(e => !e.IsValid());
        Debug.Log($"[TaskEventCollection] Cleaned invalid events. Remaining: {events.Count}");
    }

    /// <summary>
    /// Создать тестовые события
    /// </summary>
    [ContextMenu("Create Sample Events")]
    public void CreateSampleEvents()
    {
        events.Clear();
        
        events.Add(new TaskEvent
        {
            eventId = "start_mission",
            eventName = "Начало миссии",
            taskText = "Добро пожаловать! Найдите вход в здание.",
            textColor = Color.green,
            fontSize = 24
        });
        
        events.Add(new TaskEvent
        {
            eventId = "enter_building",
            eventName = "Вход в здание",
            taskText = "Осторожно! Внутри могут быть охранники.",
            textColor = Color.yellow,
            fontSize = 20,
            autoHideAfter = 5f
        });
        
        events.Add(new TaskEvent
        {
            eventId = "find_treasure",
            eventName = "Поиск сокровища",
            taskText = "Найдите сейф с сокровищами в главном зале.",
            textColor = Color.cyan,
            canRepeat = false
        });
        
        Debug.Log("[TaskEventCollection] Sample events created!");
    }

    /// <summary>
    /// Создать события для системы ограбления
    /// </summary>
    [ContextMenu("Create Heist Events")]
    public void CreateHeistEvents()
    {
        // Создаем событие для окончания таймера, если его нет
        if (!HasEvent("heist_timer_finished"))
        {
            events.Add(new TaskEvent
            {
                eventId = "heist_timer_finished",
                eventName = "Таймер ограбления закончился",
                taskText = "Время вышло! Быстро к выходу!",
                textColor = Color.red,
                fontSize = 22,
                canRepeat = false,
                autoHideAfter = 0f // Не скрывать автоматически, важное сообщение
            });
            
            Debug.Log("[TaskEventCollection] Heist timer event created!");
        }

        // Добавляем другие полезные события для ограбления
        if (!HasEvent("heist_door_opened"))
        {
            events.Add(new TaskEvent
            {
                eventId = "heist_door_opened", 
                eventName = "Дверь ограбления открыта",
                taskText = "Дверь открыта! Собирайте сокровища!",
                textColor = Color.green,
                fontSize = 20,
                autoHideAfter = 3f
            });
        }

        if (!HasEvent("escape_zone_available"))
        {
            events.Add(new TaskEvent
            {
                eventId = "escape_zone_available",
                eventName = "Зона побега доступна", 
                taskText = "Зона побега активна! Удерживайте клавишу для завершения.",
                textColor = Color.cyan,
                fontSize = 18,
                canRepeat = true
            });
        }

        Debug.Log("[TaskEventCollection] Heist events created!");
    }
}

/// <summary>
/// Настройки UI для задач
/// </summary>
[System.Serializable]
public class TaskUISettings
{
    [Header("Default Text Settings")]
    public Color defaultTextColor = Color.white;
    public int defaultFontSize = 18;
    
    [Header("Animation")]
    public bool useAnimation = true;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.3f;
    
    [Header("Position")]
    public bool centerOnScreen = true;
    public Vector2 customPosition = Vector2.zero;
}

/// <summary>
/// Результат валидации
/// </summary>
public class ValidationResult
{
    public List<string> Errors { get; } = new List<string>();
    public List<string> Warnings { get; } = new List<string>();
    
    public bool IsValid => Errors.Count == 0;
    
    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
    
    public void LogResults(string context = "")
    {
        if (!string.IsNullOrEmpty(context))
            context = $"[{context}] ";
            
        foreach (string error in Errors)
            Debug.LogError($"{context}ERROR: {error}");
            
        foreach (string warning in Warnings)
            Debug.LogWarning($"{context}WARNING: {warning}");
            
        if (IsValid && Warnings.Count == 0)
            Debug.Log($"{context}Validation passed successfully!");
    }
}