using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

/// <summary>
/// Триггер для активации событий задач при пересечении игроком
/// </summary>
[RequireComponent(typeof(Collider))]
public class TaskEventTrigger : NetworkBehaviour
{
    [Header("Event Configuration")]
    [Tooltip("Коллекция событий для выбора")]
    public TaskEventCollection eventCollection;
    
    [Tooltip("ID события которое будет активировано")]
    public string eventToTrigger;
    
    [Header("Trigger Settings")]
    [Tooltip("Только владельцы объектов могут активировать триггер")]
    public bool onlyOwnerCanTrigger = true;
    
    [Tooltip("Триггер может срабатывать только один раз")]
    public bool triggerOnce = true;
    
    [Tooltip("Задержка перед активацией события (в секундах)")]
    public float activationDelay = 0f;
    
    [Tooltip("Минимальное количество игроков в триггере для активации")]
    [Range(1, 10)]
    public int requiredPlayersCount = 1;
    
    [Header("Visual Feedback")]
    [Tooltip("Показывать визуальную обратную связь при активации")]
    public bool showVisualFeedback = true;
    
    [Tooltip("Объект эффекта активации (опционально)")]
    public GameObject activationEffect;
    
    [Tooltip("Цвет свечения при готовности к активации")]
    public Color readyColor = Color.green;
    
    [Tooltip("Цвет свечения при активации")]
    public Color activationColor = Color.yellow;
    
    [Header("Audio")]
    [Tooltip("Звук при входе в триггер")]
    public AudioClip enterSound;
    
    [Tooltip("Звук при активации")]
    public AudioClip activationSound;
    
    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool enableDebugLogs = false;

    // Внутренние переменные
    private bool hasTriggered = false;
    private readonly HashSet<PlayerController> playersInTrigger = new HashSet<PlayerController>();
    private Collider triggerCollider;
    private Renderer triggerRenderer;
    private Color originalColor;
    private Material triggerMaterial;
    
    // События для других скриптов
    public System.Action<string> OnEventTriggered;
    public System.Action<PlayerController> OnPlayerEnter;
    public System.Action<PlayerController> OnPlayerExit;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerRenderer = GetComponent<Renderer>();
        
        // Убеждаемся, что коллайдер настроен как триггер
        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            if (enableDebugLogs)
                Debug.LogWarning($"[TaskEventTrigger] {gameObject.name}: Collider wasn't set as trigger. Fixed automatically.");
        }
        
        // Сохраняем оригинальный цвет для визуальной обратной связи
        if (triggerRenderer != null)
        {
            triggerMaterial = triggerRenderer.material;
            originalColor = triggerMaterial.color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        
        // Проверяем, может ли этот игрок активировать триггер
        if (onlyOwnerCanTrigger && !player.IsOwner) return;
        
        bool wasEmpty = playersInTrigger.Count == 0;
        playersInTrigger.Add(player);
        
        if (enableDebugLogs)
            Debug.Log($"[TaskEventTrigger] Player {player.name} entered trigger. Players in trigger: {playersInTrigger.Count}");
        
        // Играем звук входа
        if (enterSound != null && player.IsOwner)
            AudioSource.PlayClipAtPoint(enterSound, transform.position);
            
        // Визуальная обратная связь
        if (showVisualFeedback && wasEmpty)
            UpdateVisualState();
            
        // Уведомляем подписчиков
        OnPlayerEnter?.Invoke(player);
        
        // Проверяем, можно ли активировать событие
        CheckActivationConditions(player);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        
        playersInTrigger.Remove(player);
        
        if (enableDebugLogs)
            Debug.Log($"[TaskEventTrigger] Player {player.name} exited trigger. Players in trigger: {playersInTrigger.Count}");
        
        // Визуальная обратная связь
        if (showVisualFeedback && playersInTrigger.Count == 0)
            UpdateVisualState();
            
        // Уведомляем подписчиков
        OnPlayerExit?.Invoke(player);
    }

    private void CheckActivationConditions(PlayerController triggeringPlayer)
    {
        // Проверяем, уже ли сработал триггер
        if (hasTriggered && triggerOnce) return;
        
        // Проверяем количество игроков
        if (playersInTrigger.Count < requiredPlayersCount) return;
        
        // Проверяем, есть ли событие для активации
        if (eventCollection == null || string.IsNullOrEmpty(eventToTrigger))
        {
            Debug.LogWarning($"[TaskEventTrigger] {gameObject.name}: No event collection or event ID specified!");
            return;
        }
        
        TaskEvent eventData = eventCollection.GetEvent(eventToTrigger);
        if (eventData == null)
        {
            Debug.LogError($"[TaskEventTrigger] {gameObject.name}: Event '{eventToTrigger}' not found in collection!");
            return;
        }
        
        // Проверяем условия события
        if (eventData.requireMinPlayers && playersInTrigger.Count < eventData.minPlayersRequired)
            return;
            
        if (!eventData.canRepeat && hasTriggered)
            return;
        
        // Активируем событие
        if (activationDelay > 0f)
        {
            Invoke(nameof(ActivateEventDelayed), activationDelay);
        }
        else
        {
            ActivateEvent(triggeringPlayer);
        }
    }

    private void ActivateEventDelayed()
    {
        // Находим любого игрока в триггере для активации
        PlayerController triggeringPlayer = null;
        foreach (var player in playersInTrigger)
        {
            if (player != null)
            {
                triggeringPlayer = player;
                break;
            }
        }
        
        if (triggeringPlayer != null)
            ActivateEvent(triggeringPlayer);
    }

    private void ActivateEvent(PlayerController triggeringPlayer)
    {
        if (!IsServer) return; // Только сервер может активировать события
        
        hasTriggered = true;
        
        if (enableDebugLogs)
            Debug.Log($"[TaskEventTrigger] Activating event '{eventToTrigger}' triggered by {triggeringPlayer.name}");
        
        // Находим менеджер задач и активируем событие
        TaskManager taskManager = FindObjectOfType<TaskManager>();
        if (taskManager != null)
        {
            taskManager.TriggerEventServerRpc(eventToTrigger, triggeringPlayer.NetworkObject);
        }
        else
        {
            Debug.LogError("[TaskEventTrigger] TaskManager not found! Cannot activate event.");
            return;
        }
        
        // Визуальные и звуковые эффекты
        ShowActivationEffects();
        
        // Уведомляем подписчиков
        OnEventTriggered?.Invoke(eventToTrigger);
        
        // Отключаем триггер если он одноразовый
        if (triggerOnce)
        {
            enabled = false;
            if (enableDebugLogs)
                Debug.Log($"[TaskEventTrigger] {gameObject.name} disabled after single use");
        }
    }

    private void ShowActivationEffects()
    {
        // Визуальная обратная связь
        if (showVisualFeedback)
            UpdateVisualState(true);
        
        // Эффект активации
        if (activationEffect != null)
        {
            GameObject effect = Instantiate(activationEffect, transform.position, transform.rotation);
            Destroy(effect, 5f); // Удаляем эффект через 5 секунд
        }
        
        // Звук активации (играется для всех)
        AudioClip soundToPlay = activationSound ?? (eventCollection?.GetEvent(eventToTrigger)?.activationSound);
        if (soundToPlay != null)
        {
            // Играем звук через сетевую систему для всех игроков
            PlayActivationSoundObserversRpc(soundToPlay.name);
        }
    }

    [ObserversRpc]
    private void PlayActivationSoundObserversRpc(string soundName)
    {
        // Здесь должна быть логика воспроизведения звука по имени
        if (enableDebugLogs)
            Debug.Log($"[TaskEventTrigger] Playing activation sound: {soundName}");
    }

    private void UpdateVisualState(bool isActivating = false)
    {
        if (triggerMaterial == null) return;
        
        if (isActivating)
        {
            triggerMaterial.color = activationColor;
            // Можно добавить эмиссию или другие эффекты
        }
        else if (playersInTrigger.Count > 0)
        {
            triggerMaterial.color = readyColor;
        }
        else
        {
            triggerMaterial.color = originalColor;
        }
    }

    /// <summary>
    /// Принудительно активировать событие (для внешних скриптов)
    /// </summary>
    public void ForceActivateEvent()
    {
        if (!IsServer) return;
        
        if (playersInTrigger.Count > 0)
        {
            PlayerController firstPlayer = null;
            foreach (var player in playersInTrigger)
            {
                if (player != null)
                {
                    firstPlayer = player;
                    break;
                }
            }
            
            if (firstPlayer != null)
                ActivateEvent(firstPlayer);
        }
    }

    /// <summary>
    /// Сбросить состояние триггера
    /// </summary>
    [ContextMenu("Reset Trigger")]
    public void ResetTrigger()
    {
        hasTriggered = false;
        enabled = true;
        UpdateVisualState();
        
        if (enableDebugLogs)
            Debug.Log($"[TaskEventTrigger] {gameObject.name} reset");
    }

    /// <summary>
    /// Получить информацию о текущем состоянии
    /// </summary>
    public string GetStatusInfo()
    {
        return $"Event: {eventToTrigger}, Players: {playersInTrigger.Count}/{requiredPlayersCount}, Triggered: {hasTriggered}";
    }

    private void OnValidate()
    {
        // Проверяем наличие коллайдера
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError($"[TaskEventTrigger] {gameObject.name}: Missing Collider component!");
        }
        
        // Проверяем, что коллайдер настроен как триггер
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[TaskEventTrigger] {gameObject.name}: Collider should be set as Trigger!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Показываем границы триггера в редакторе
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.red : (playersInTrigger.Count > 0 ? readyColor : Color.cyan);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2));
            }
        }
    }
}