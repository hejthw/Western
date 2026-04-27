#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using FishNet.Component.Transforming;

/// <summary>
/// Утилита для обновления префаба игрока: отключение NetworkTransform и добавление SimplePlayerSync
/// </summary>
public class PlayerPrefabUpdater : MonoBehaviour
{
    [MenuItem("Tools/Player Sync/Update Player Prefab for SimplePlayerSync")]
    public static void UpdatePlayerPrefab()
    {
        // Путь к префабу игрока
        string prefabPath = "Assets/Prefab/Player.prefab";
        
        // Загружаем префаб
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[PlayerPrefabUpdater] Префаб не найден по пути: {prefabPath}");
            return;
        }

        // Создаем экземпляр для редактирования
        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (prefabInstance == null)
        {
            Debug.LogError("[PlayerPrefabUpdater] Не удалось создать экземпляр префаба");
            return;
        }

        bool changesMade = false;

        try
        {
            // Отключаем NetworkTransform (но не удаляем для совместимости)
            NetworkTransform networkTransform = prefabInstance.GetComponent<NetworkTransform>();
            if (networkTransform != null && networkTransform.enabled)
            {
                networkTransform.enabled = false;
                Debug.Log("[PlayerPrefabUpdater] NetworkTransform отключен");
                changesMade = true;
            }

            // Добавляем SimplePlayerSync_FishNet4 если его нет
            SimplePlayerSync_FishNet4 simpleSync = prefabInstance.GetComponent<SimplePlayerSync_FishNet4>();
            if (simpleSync == null)
            {
                simpleSync = prefabInstance.AddComponent<SimplePlayerSync_FishNet4>();
                Debug.Log("[PlayerPrefabUpdater] SimplePlayerSync_FishNet4 добавлен");
                changesMade = true;
                
                // Настраиваем параметры для тестирования
                SerializedObject so = new SerializedObject(simpleSync);
                so.FindProperty("positionThreshold").floatValue = 0.1f;
                so.FindProperty("rotationThreshold").floatValue = 5f;
                so.FindProperty("lerpSpeed").floatValue = 15f;
                so.FindProperty("teleportDistance").floatValue = 5f;
                so.FindProperty("forceSyncInterval").floatValue = 1f;
                so.FindProperty("enableDebugLogs").boolValue = true; // Включаем отладку для тестирования
                so.ApplyModifiedProperties();
                
                Debug.Log("[PlayerPrefabUpdater] SimplePlayerSync_FishNet4 настроен с параметрами по умолчанию");
            }

            // Сохраняем изменения в префаб
            if (changesMade)
            {
                PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
                Debug.Log("[PlayerPrefabUpdater] Изменения сохранены в префаб");
                
                // Помечаем префаб как измененный
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                
                Debug.Log("<color=green>[PlayerPrefabUpdater] Префаб игрока успешно обновлен!</color>");
                Debug.Log("<color=yellow>Рекомендуется протестировать синхронизацию в сетевой игре</color>");
            }
            else
            {
                Debug.Log("[PlayerPrefabUpdater] Префаб уже обновлен, изменения не требуются");
            }
        }
        finally
        {
            // Удаляем временный экземпляр
            DestroyImmediate(prefabInstance);
        }
    }

    [MenuItem("Tools/Player Sync/Revert to NetworkTransform")]
    public static void RevertToNetworkTransform()
    {
        string prefabPath = "Assets/Prefab/Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"[PlayerPrefabUpdater] Префаб не найден по пути: {prefabPath}");
            return;
        }

        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (prefabInstance == null)
        {
            Debug.LogError("[PlayerPrefabUpdater] Не удалось создать экземпляр префаба");
            return;
        }

        bool changesMade = false;

        try
        {
            // Включаем NetworkTransform
            NetworkTransform networkTransform = prefabInstance.GetComponent<NetworkTransform>();
            if (networkTransform != null && !networkTransform.enabled)
            {
                networkTransform.enabled = true;
                Debug.Log("[PlayerPrefabUpdater] NetworkTransform включен");
                changesMade = true;
            }

            // Отключаем SimplePlayerSync_FishNet4 (но не удаляем)
            SimplePlayerSync_FishNet4 simpleSync = prefabInstance.GetComponent<SimplePlayerSync_FishNet4>();
            if (simpleSync != null && simpleSync.enabled)
            {
                simpleSync.enabled = false;
                Debug.Log("[PlayerPrefabUpdater] SimplePlayerSync_FishNet4 отключен");
                changesMade = true;
            }

            if (changesMade)
            {
                PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                
                Debug.Log("<color=green>[PlayerPrefabUpdater] Возврат к NetworkTransform выполнен!</color>");
            }
            else
            {
                Debug.Log("[PlayerPrefabUpdater] Уже используется NetworkTransform");
            }
        }
        finally
        {
            DestroyImmediate(prefabInstance);
        }
    }

    [MenuItem("Tools/Player Sync/Show Current Sync Status")]
    public static void ShowCurrentSyncStatus()
    {
        string prefabPath = "Assets/Prefab/Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"[PlayerPrefabUpdater] Префаб не найден по пути: {prefabPath}");
            return;
        }

        NetworkTransform nt = prefab.GetComponent<NetworkTransform>();
        SimplePlayerSync_FishNet4 sps = prefab.GetComponent<SimplePlayerSync_FishNet4>();

        Debug.Log("=== СТАТУС СИНХРОНИЗАЦИИ ИГРОКА ===");
        
        if (nt != null)
        {
            Debug.Log($"NetworkTransform: {(nt.enabled ? "<color=green>ВКЛЮЧЕН</color>" : "<color=red>ОТКЛЮЧЕН</color>")}");
        }
        else
        {
            Debug.Log("NetworkTransform: <color=red>НЕ НАЙДЕН</color>");
        }

        if (sps != null)
        {
            Debug.Log($"SimplePlayerSync_FishNet4: {(sps.enabled ? "<color=green>ВКЛЮЧЕН</color>" : "<color=red>ОТКЛЮЧЕН</color>")}");
        }
        else
        {
            Debug.Log("SimplePlayerSync_FishNet4: <color=red>НЕ НАЙДЕН</color>");
        }

        string activeSystem = "НЕОПРЕДЕЛЕНА";
        if (sps != null && sps.enabled)
        {
            activeSystem = "<color=green>SimplePlayerSync_FishNet4 (Новая система)</color>";
        }
        else if (nt != null && nt.enabled)
        {
            activeSystem = "<color=yellow>NetworkTransform (Старая система)</color>";
        }
        else
        {
            activeSystem = "<color=red>ОТСУТСТВУЕТ</color>";
        }

        Debug.Log($"Активная система синхронизации: {activeSystem}");
        Debug.Log("=====================================");
    }
}
#endif