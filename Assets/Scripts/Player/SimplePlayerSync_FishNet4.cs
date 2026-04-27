using FishNet.Object;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// Простая система синхронизации позиции игрока для FishNet 4.
/// Решает проблему конфликтов между PlayerPhysics и NetworkTransform.
/// Использует только RPC-систему без устаревших SyncVar.
/// </summary>
public class SimplePlayerSync_FishNet4 : NetworkBehaviour
{
    [Header("Sync Settings")]
    [Tooltip("Минимальное расстояние для отправки обновления позиции")]
    [SerializeField] private float positionThreshold = 0.1f;
    
    [Tooltip("Минимальный угол поворота для отправки обновления вращения")]
    [SerializeField] private float rotationThreshold = 5f;
    
    [Tooltip("Скорость интерполяции для удаленных игроков")]
    [SerializeField] private float lerpSpeed = 15f;
    
    [Tooltip("Максимальное расстояние для телепортации вместо интерполяции")]
    [SerializeField] private float teleportDistance = 5f;
    
    [Tooltip("Интервал принудительной синхронизации (в секундах)")]
    [SerializeField] private float forceSyncInterval = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Локальные переменные для владельца
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;
    private float lastForceSyncTime;
    
    // Переменные для интерполяции (удаленные клиенты)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool needsInterpolation;

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (IsOwner)
        {
            // Инициализация для владельца
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            
            // Отправляем начальную позицию на сервер
            SendTransformUpdateServerRpc(transform.position, transform.rotation);
            
            if (enableDebugLogs)
                Debug.Log($"[SimplePlayerSync_FishNet4] Owner initialized at {transform.position}");
        }
        else
        {
            // Инициализация для удаленных копий
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            
            if (enableDebugLogs)
                Debug.Log($"[SimplePlayerSync_FishNet4] Remote player initialized at {transform.position}");
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            HandleOwnerSync();
        }
        else
        {
            HandleRemoteInterpolation();
        }
    }

    /// <summary>
    /// Обработка синхронизации для владельца объекта
    /// </summary>
    private void HandleOwnerSync()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;
        
        // Проверяем, нужно ли отправить обновление
        bool positionChanged = Vector3.Distance(currentPosition, lastSentPosition) > positionThreshold;
        bool rotationChanged = Quaternion.Angle(currentRotation, lastSentRotation) > rotationThreshold;
        bool forceSyncNeeded = Time.time - lastForceSyncTime > forceSyncInterval;
        
        if (positionChanged || rotationChanged || forceSyncNeeded)
        {
            SendTransformUpdateServerRpc(currentPosition, currentRotation);
            lastSentPosition = currentPosition;
            lastSentRotation = currentRotation;
            
            if (forceSyncNeeded)
                lastForceSyncTime = Time.time;
                
            if (enableDebugLogs && (positionChanged || rotationChanged))
            {
                Debug.Log($"[SimplePlayerSync_FishNet4] Owner sent update: pos={currentPosition}, rot={currentRotation.eulerAngles}");
            }
        }
    }

    /// <summary>
    /// Обработка интерполяции для удаленных копий
    /// </summary>
    private void HandleRemoteInterpolation()
    {
        if (!needsInterpolation) return;
        
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Если расстояние слишком большое, телепортируемся
        if (distance > teleportDistance)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            needsInterpolation = false;
            
            if (enableDebugLogs)
                Debug.Log($"[SimplePlayerSync_FishNet4] Remote player teleported to {targetPosition}");
        }
        else
        {
            // Плавная интерполяция
            transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lerpSpeed * Time.deltaTime);
            
            // Останавливаем интерполяцию, когда достигли цели
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f && 
                Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                needsInterpolation = false;
            }
        }
    }

    /// <summary>
    /// ServerRPC для получения обновлений позиции от клиента
    /// </summary>
    [ServerRpc]
    private void SendTransformUpdateServerRpc(Vector3 position, Quaternion rotation)
    {
        // Простая валидация на сервере
        if (IsValidMovement(position))
        {
            // Отправляем обновление всем наблюдателям (кроме отправителя)
            ReceiveTransformUpdateObserversRpc(position, rotation);
            
            if (enableDebugLogs)
                Debug.Log($"[SimplePlayerSync_FishNet4] Server relayed position {position} to observers");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning($"[SimplePlayerSync_FishNet4] Invalid movement detected: {position}");
        }
    }

    /// <summary>
    /// ObserversRPC для получения обновлений позиции удаленными клиентами
    /// </summary>
    [ObserversRpc(ExcludeOwner = true)]
    private void ReceiveTransformUpdateObserversRpc(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
        needsInterpolation = true;
        
        if (enableDebugLogs)
            Debug.Log($"[SimplePlayerSync_FishNet4] Remote received update: target={position}");
    }

    /// <summary>
    /// Простая валидация движения (можно расширить для защиты от читов)
    /// </summary>
    private bool IsValidMovement(Vector3 newPosition)
    {
        // Проверяем, что игрок не двигается слишком быстро
        float maxDistance = 20f; // Максимальное расстояние за один кадр
        float distance = Vector3.Distance(lastSentPosition, newPosition);
        
        return distance <= maxDistance;
    }

    /// <summary>
    /// Принудительная синхронизация позиции (для использования в других скриптах)
    /// </summary>
    public void ForceSync()
    {
        if (IsOwner)
        {
            SendTransformUpdateServerRpc(transform.position, transform.rotation);
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;
            
            if (enableDebugLogs)
                Debug.Log("[SimplePlayerSync_FishNet4] Force sync executed");
        }
    }

    /// <summary>
    /// Телепортация (для использования в других скриптах)
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        
        if (IsOwner)
        {
            SendTransformUpdateServerRpc(position, rotation);
            lastSentPosition = position;
            lastSentRotation = rotation;
        }
        else
        {
            targetPosition = position;
            targetRotation = rotation;
            needsInterpolation = false;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SimplePlayerSync_FishNet4] Teleported to {position}");
    }

    /// <summary>
    /// Серверный метод для принудительной телепортации всех клиентов
    /// </summary>
    public void ServerTeleportAll(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;
        
        // Отправляем телепортацию всем клиентам включая владельца
        ServerTeleportObserversRpc(position, rotation);
        
        if (enableDebugLogs)
            Debug.Log($"[SimplePlayerSync_FishNet4] Server teleported all clients to {position}");
    }

    /// <summary>
    /// RPC для серверной телепортации всех клиентов
    /// </summary>
    [ObserversRpc]
    private void ServerTeleportObserversRpc(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        targetPosition = position;
        targetRotation = rotation;
        needsInterpolation = false;
        
        if (IsOwner)
        {
            lastSentPosition = position;
            lastSentRotation = rotation;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SimplePlayerSync_FishNet4] Client received server teleport to {position}");
    }

    /// <summary>
    /// Получить текущую целевую позицию (для отладки)
    /// </summary>
    public Vector3 GetTargetPosition() => IsOwner ? transform.position : targetPosition;
    
    /// <summary>
    /// Получить текущее целевое вращение (для отладки)
    /// </summary>
    public Quaternion GetTargetRotation() => IsOwner ? transform.rotation : targetRotation;

    /// <summary>
    /// Проверка, идет ли интерполяция (для отладки)
    /// </summary>
    public bool IsInterpolating() => needsInterpolation;
}