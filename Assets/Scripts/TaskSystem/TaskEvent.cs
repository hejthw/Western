using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Данные об одном событии задачи
/// </summary>
[Serializable]
public class TaskEvent
{
    [Header("Event Info")]
    [Tooltip("Уникальный ID события")]
    public string eventId;
    
    [Tooltip("Название события для редактора")]
    public string eventName;
    
    [TextArea(3, 6)]
    [Tooltip("Текст задачи, который будет отображаться игрокам")]
    public string taskText;
    
    [Header("UI Settings")]
    [Tooltip("Цвет текста задачи (опционально)")]
    public Color textColor = Color.white;
    
    [Tooltip("Размер шрифта (0 = использовать по умолчанию)")]
    [Range(0, 72)]
    public int fontSize = 0;
    
    [Header("Audio")]
    [Tooltip("Звук при активации события (опционально)")]
    public AudioClip activationSound;
    
    [Tooltip("Громкость звука")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    [Header("Timing")]
    [Tooltip("Автоматически скрыть задачу через N секунд (0 = не скрывать)")]
    public float autoHideAfter = 0f;
    
    [Header("Conditions")]
    [Tooltip("Можно ли активировать это событие повторно")]
    public bool canRepeat = true;
    
    [Tooltip("Требуется ли определенное количество игроков для активации")]
    public bool requireMinPlayers = false;
    
    [Tooltip("Минимальное количество игроков")]
    [Range(1, 10)]
    public int minPlayersRequired = 1;

    /// <summary>
    /// Проверяет, валидно ли событие
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(eventId) && !string.IsNullOrEmpty(taskText);
    }

    /// <summary>
    /// Применяет настройки к компоненту текста
    /// </summary>
    public void ApplyToTextComponent(TextMeshProUGUI textComponent, bool preserveStyle = false)
    {
        if (textComponent == null) return;
        
        // Сохраняем оригинальные настройки если нужно
        Color originalColor = textComponent.color;
        float originalFontSize = textComponent.fontSize;
        
        // Всегда меняем текст
        textComponent.text = taskText;
        
        // Применяем стиль только если не нужно сохранять оригинал
        if (!preserveStyle)
        {
            textComponent.color = textColor;
            
            if (fontSize > 0)
                textComponent.fontSize = fontSize;
        }
    }

    /// <summary>
    /// Создает копию события с новым ID
    /// </summary>
    public TaskEvent Clone(string newId = null)
    {
        TaskEvent clone = new TaskEvent
        {
            eventId = newId ?? eventId + "_clone",
            eventName = eventName + " (Copy)",
            taskText = taskText,
            textColor = textColor,
            fontSize = fontSize,
            activationSound = activationSound,
            soundVolume = soundVolume,
            autoHideAfter = autoHideAfter,
            canRepeat = canRepeat,
            requireMinPlayers = requireMinPlayers,
            minPlayersRequired = minPlayersRequired
        };
        return clone;
    }
}