using UnityEngine;
using FishNet.Object;
using System.Collections;

/// <summary>
/// Контроллер для управления CashZone через триггеры
/// </summary>
public class CashZoneController : NetworkBehaviour
{
    [Header("Position Control")]
    [Tooltip("Новая позиция CashZone после активации")]
    public Transform newPosition;
    
    [Tooltip("Скорость перемещения")]
    public float moveSpeed = 2f;
    
    [Tooltip("Использовать плавное перемещение")]
    public bool smoothMovement = true;
    
    [Tooltip("Задержка перед перемещением")]
    public float delayBeforeMove = 0f;

    [Header("Visibility Control")]
    [Tooltip("Скрыть CashZone после активации")]
    public bool hideAfterActivation = false;
    
    [Tooltip("Показать CashZone после активации")]
    public bool showAfterActivation = false;

    [Header("Audio")]
    [Tooltip("Звук при перемещении")]
    public AudioClip moveSound;
    
    [Tooltip("Громкость звука")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool enableDebugLogs = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isActivated = false;
    private CashZone cashZone;

    private void Awake()
    {
        cashZone = GetComponent<CashZone>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    /// <summary>
    /// Активировать изменения CashZone (вызывается из UnityEvent)
    /// </summary>
    public void ActivateCashZoneChanges()
    {
        if (isActivated)
        {
            if (enableDebugLogs)
                Debug.Log("[CashZoneController] Already activated, ignoring");
            return;
        }

        if (!IsServer)
        {
            if (enableDebugLogs)
                Debug.Log("[CashZoneController] Not server, ignoring activation");
            return;
        }

        isActivated = true;

        if (enableDebugLogs)
            Debug.Log($"[CashZoneController] Activating cash zone changes on {gameObject.name}");

        // Запускаем изменения с задержкой если нужно
        if (delayBeforeMove > 0f)
        {
            StartCoroutine(DelayedActivation());
        }
        else
        {
            ExecuteChanges();
        }
    }

    private IEnumerator DelayedActivation()
    {
        yield return new WaitForSeconds(delayBeforeMove);
        ExecuteChanges();
    }

    private void ExecuteChanges()
    {
        // Синхронизируем изменения со всеми клиентами
        ExecuteChangesObserversRpc();
    }

    [ObserversRpc]
    private void ExecuteChangesObserversRpc()
    {
        StartCoroutine(ExecuteChangesCoroutine());
    }

    private IEnumerator ExecuteChangesCoroutine()
    {
        // Воспроизводим звук
        if (moveSound != null)
        {
            AudioSource.PlayClipAtPoint(moveSound, transform.position, soundVolume);
        }

        // Скрытие/показ
        if (hideAfterActivation)
        {
            gameObject.SetActive(false);
            if (enableDebugLogs)
                Debug.Log("[CashZoneController] Cash zone hidden");
            yield break;
        }
        
        if (showAfterActivation)
        {
            gameObject.SetActive(true);
            if (enableDebugLogs)
                Debug.Log("[CashZoneController] Cash zone shown");
        }

        // Перемещение
        if (newPosition != null)
        {
            Vector3 targetPos = newPosition.position;
            Quaternion targetRot = newPosition.rotation;

            if (smoothMovement)
            {
                // Плавное перемещение
                float elapsed = 0f;
                Vector3 startPos = transform.position;
                Quaternion startRot = transform.rotation;

                while (elapsed < 1f)
                {
                    elapsed += Time.deltaTime * moveSpeed;
                    
                    transform.position = Vector3.Lerp(startPos, targetPos, elapsed);
                    transform.rotation = Quaternion.Lerp(startRot, targetRot, elapsed);
                    
                    yield return null;
                }

                // Убеждаемся, что достигли точной позиции
                transform.position = targetPos;
                transform.rotation = targetRot;
            }
            else
            {
                // Мгновенное перемещение
                transform.position = targetPos;
                transform.rotation = targetRot;
            }

            if (enableDebugLogs)
                Debug.Log($"[CashZoneController] Moved to position: {targetPos}");
        }
    }

    /// <summary>
    /// Сбросить CashZone в исходную позицию (для отладки)
    /// </summary>
    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        if (!IsServer) return;

        isActivated = false;
        ResetPositionObserversRpc();
    }

    [ObserversRpc]
    private void ResetPositionObserversRpc()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        gameObject.SetActive(true);
        
        if (enableDebugLogs)
            Debug.Log("[CashZoneController] Position reset");
    }

    /// <summary>
    /// Проверить, была ли активирована
    /// </summary>
    public bool IsActivated() => isActivated;

    /// <summary>
    /// Установить новую позицию программно
    /// </summary>
    public void SetNewPosition(Transform position)
    {
        newPosition = position;
    }

    /// <summary>
    /// Установить новую позицию по координатам
    /// </summary>
    public void SetNewPosition(Vector3 position, Quaternion rotation)
    {
        if (newPosition == null)
        {
            GameObject posMarker = new GameObject($"{gameObject.name}_NewPosition");
            newPosition = posMarker.transform;
        }
        
        newPosition.position = position;
        newPosition.rotation = rotation;
    }

    private void OnDrawGizmosSelected()
    {
        // Показываем связь между исходной и новой позицией
        if (newPosition != null)
        {
            Gizmos.color = isActivated ? Color.green : Color.yellow;
            
            // Линия между позициями
            Gizmos.DrawLine(transform.position, newPosition.position);
            
            // Маркер новой позиции
            Gizmos.DrawWireCube(newPosition.position, Vector3.one * 0.5f);
            
            // Стрелка направления
            Vector3 direction = (newPosition.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Gizmos.DrawRay(transform.position, direction * Vector3.Distance(transform.position, newPosition.position) * 0.8f);
            }
        }
    }
}