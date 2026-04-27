#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Редактор для системы задач - предоставляет удобные инструменты настройки
/// </summary>
public class TaskSystemEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Collections", "Triggers", "UI Setup", "Testing" };
    
    // Переменные для разных вкладок
    private TaskEventCollection selectedCollection;
    private TaskEventTrigger selectedTrigger;
    private TaskUIController selectedUIController;
    private GameObject selectedUIElement;
    
    // Переменные для создания новых элементов
    private string newEventId = "";
    private string newEventName = "";
    private string newTaskText = "";

    [MenuItem("Tools/Task System/Task System Editor")]
    public static void ShowWindow()
    {
        TaskSystemEditor window = GetWindow<TaskSystemEditor>("Task System Editor");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Task System Editor", EditorStyles.largeLabel);
        EditorGUILayout.Space();
        
        // Вкладки
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        switch (selectedTab)
        {
            case 0: DrawCollectionsTab(); break;
            case 1: DrawTriggersTab(); break;
            case 2: DrawUISetupTab(); break;
            case 3: DrawTestingTab(); break;
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawCollectionsTab()
    {
        EditorGUILayout.LabelField("Event Collections Management", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Выбор коллекции
        selectedCollection = EditorGUILayout.ObjectField("Selected Collection", selectedCollection, typeof(TaskEventCollection), false) as TaskEventCollection;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Collection"))
        {
            CreateNewCollection();
        }
        
        if (GUILayout.Button("Find All Collections"))
        {
            FindAllCollections();
        }
        EditorGUILayout.EndHorizontal();
        
        if (selectedCollection != null)
        {
            EditorGUILayout.Space();
            DrawCollectionInfo();
            EditorGUILayout.Space();
            DrawEventCreation();
        }
    }

    private void DrawCollectionInfo()
    {
        EditorGUILayout.LabelField($"Collection: {selectedCollection.name}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Events count: {selectedCollection.events.Count}");
        
        if (selectedCollection.events.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events:", EditorStyles.boldLabel);
            
            foreach (var taskEvent in selectedCollection.events)
            {
                if (taskEvent.IsValid())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {taskEvent.eventId}: {taskEvent.eventName}");
                    
                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    {
                        Selection.activeObject = selectedCollection;
                    }
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Event", 
                            $"Are you sure you want to remove event '{taskEvent.eventId}'?", 
                            "Yes", "No"))
                        {
                            selectedCollection.RemoveEvent(taskEvent.eventId);
                            EditorUtility.SetDirty(selectedCollection);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Validate Collection"))
        {
            var result = selectedCollection.Validate();
            result.LogResults("TaskSystemEditor");
        }
    }

    private void DrawEventCreation()
    {
        EditorGUILayout.LabelField("Create New Event", EditorStyles.boldLabel);
        
        newEventId = EditorGUILayout.TextField("Event ID", newEventId);
        newEventName = EditorGUILayout.TextField("Event Name", newEventName);
        newTaskText = EditorGUILayout.TextArea(newTaskText, GUILayout.Height(60));
        
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newEventId) || string.IsNullOrEmpty(newTaskText));
        if (GUILayout.Button("Add Event to Collection"))
        {
            TaskEvent newEvent = new TaskEvent
            {
                eventId = newEventId,
                eventName = string.IsNullOrEmpty(newEventName) ? newEventId : newEventName,
                taskText = newTaskText,
                textColor = Color.white,
                canRepeat = true
            };
            
            selectedCollection.AddEvent(newEvent);
            EditorUtility.SetDirty(selectedCollection);
            
            // Очищаем поля
            newEventId = "";
            newEventName = "";
            newTaskText = "";
            
            GUI.FocusControl(null);
        }
        EditorGUI.EndDisabledGroup();
    }

    private void DrawTriggersTab()
    {
        EditorGUILayout.LabelField("Trigger Management", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Поиск всех триггеров в сцене
        TaskEventTrigger[] allTriggers = FindObjectsOfType<TaskEventTrigger>();
        
        if (allTriggers.Length == 0)
        {
            EditorGUILayout.HelpBox("No TaskEventTriggers found in the scene.", MessageType.Info);
            
            if (GUILayout.Button("Create Task Event Trigger"))
            {
                CreateTaskEventTrigger();
            }
            return;
        }
        
        EditorGUILayout.LabelField($"Found {allTriggers.Length} trigger(s) in scene:");
        
        foreach (var trigger in allTriggers)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(trigger.gameObject.name, EditorStyles.boldLabel);
            
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = trigger.gameObject;
            }
            
            if (GUILayout.Button("Configure", GUILayout.Width(80)))
            {
                selectedTrigger = trigger;
                Selection.activeObject = trigger.gameObject;
                selectedTab = 1; // Остаемся на вкладке триггеров
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Показываем информацию о триггере
            EditorGUILayout.LabelField($"Event: {trigger.eventToTrigger}");
            EditorGUILayout.LabelField($"Collection: {(trigger.eventCollection ? trigger.eventCollection.name : "None")}");
            EditorGUILayout.LabelField($"Status: {trigger.GetStatusInfo()}");
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Create New Trigger"))
        {
            CreateTaskEventTrigger();
        }
    }

    private void DrawUISetupTab()
    {
        EditorGUILayout.LabelField("UI Setup Assistant", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Поиск UI контроллера
        if (selectedUIController == null)
            selectedUIController = FindObjectOfType<TaskUIController>();
        
        selectedUIController = EditorGUILayout.ObjectField("UI Controller", selectedUIController, typeof(TaskUIController), true) as TaskUIController;
        
        if (selectedUIController == null)
        {
            EditorGUILayout.HelpBox("No TaskUIController found in the scene.", MessageType.Warning);
            
        if (GUILayout.Button("Create Task UI Controller"))
        {
            CreateTaskUIController();
        }
        
        // Проверяем Bootstrap
        TaskSystemBootstrap bootstrap = FindObjectOfType<TaskSystemBootstrap>();
        if (bootstrap == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("TaskSystemBootstrap not found. It's recommended for proper initialization.", MessageType.Info);
            
            if (GUILayout.Button("Create Task System Bootstrap"))
            {
                CreateTaskSystemBootstrap();
            }
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("✓ TaskSystemBootstrap found", EditorStyles.boldLabel);
        }
            
            return;
        }
        
        // Помощник по настройке UI
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Element Assignment", EditorStyles.boldLabel);
        
        // Поиск текстовых элементов
        selectedUIElement = EditorGUILayout.ObjectField("UI Element (Text)", selectedUIElement, typeof(GameObject), true) as GameObject;
        
        if (selectedUIElement != null)
        {
            TextMeshProUGUI textComponent = selectedUIElement.GetComponent<TextMeshProUGUI>();
            
            if (textComponent != null)
            {
                EditorGUILayout.LabelField($"Found TextMeshPro component: {textComponent.name}");
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Set as Main Task Text"))
                {
                    Undo.RecordObject(selectedUIController, "Set Main Task Text");
                    selectedUIController.mainTaskText = textComponent;
                    EditorUtility.SetDirty(selectedUIController);
                }
                
                if (GUILayout.Button("Add to Additional Texts"))
                {
                    Undo.RecordObject(selectedUIController, "Add Additional Task Text");
                    if (!selectedUIController.additionalTaskTexts.Contains(textComponent))
                    {
                        selectedUIController.additionalTaskTexts.Add(textComponent);
                        EditorUtility.SetDirty(selectedUIController);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Selected object doesn't have a TextMeshProUGUI component.", MessageType.Warning);
            }
        }
        
        // Быстрые действия
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Find Text Elements in Scene"))
        {
            AutoFindTextElements();
        }
        
        if (GUILayout.Button("Setup Default Task Panel"))
        {
            SetupDefaultTaskPanel();
        }
        
        if (GUILayout.Button("Validate UI Setup"))
        {
            ValidateUISetup();
        }
    }

    private void DrawTestingTab()
    {
        EditorGUILayout.LabelField("System Testing", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Testing is only available in Play Mode.", MessageType.Info);
            return;
        }
        
        TaskManager taskManager = FindObjectOfType<TaskManager>();
        if (taskManager == null)
        {
            EditorGUILayout.HelpBox("TaskManager not found in scene.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField("Task Manager Status:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Active Tasks: {taskManager.GetActiveTaskIds().Count}/{taskManager.maxConcurrentTasks}");
        EditorGUILayout.LabelField($"Log Events: {taskManager.logTaskEvents}");
        
        EditorGUILayout.Space();
        
        // Тестовые кнопки
        if (selectedCollection != null && selectedCollection.events.Count > 0)
        {
            EditorGUILayout.LabelField("Test Events:", EditorStyles.boldLabel);
            
            foreach (var taskEvent in selectedCollection.events.Take(5)) // Показываем только первые 5
            {
                if (GUILayout.Button($"Trigger: {taskEvent.eventId}"))
                {
                    if (taskManager.IsServer)
                    {
                        // Находим любого игрока для теста
                        PlayerController[] players = FindObjectsOfType<PlayerController>();
                        if (players.Length > 0)
                        {
                            taskManager.TriggerEventServerRpc(taskEvent.eventId, players[0].NetworkObject);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Can only trigger events from server in network mode");
                    }
                }
            }
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Clear All Tasks"))
        {
            if (taskManager.IsServer)
                taskManager.ClearAllTasksServerRpc();
        }
        
        if (GUILayout.Button("Show Task Manager Debug Info"))
        {
            Debug.Log(taskManager.GetSystemInfo());
        }
    }

    private void CreateNewCollection()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Task Event Collection", "NewTaskEventCollection", "asset", "Create new task event collection");
        
        if (!string.IsNullOrEmpty(path))
        {
            TaskEventCollection newCollection = CreateInstance<TaskEventCollection>();
            newCollection.collectionName = "New Task Event Collection";
            newCollection.description = "Created with Task System Editor";
            
            AssetDatabase.CreateAsset(newCollection, path);
            AssetDatabase.SaveAssets();
            
            selectedCollection = newCollection;
            Selection.activeObject = newCollection;
        }
    }

    private void FindAllCollections()
    {
        string[] guids = AssetDatabase.FindAssets("t:TaskEventCollection");
        Debug.Log($"Found {guids.Length} TaskEventCollection assets:");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TaskEventCollection collection = AssetDatabase.LoadAssetAtPath<TaskEventCollection>(path);
            Debug.Log($"- {collection.name} at {path}");
        }
    }

    private void CreateTaskEventTrigger()
    {
        GameObject triggerGO = new GameObject("Task Event Trigger");
        
        // Добавляем коллайдер
        BoxCollider collider = triggerGO.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = Vector3.one * 2f;
        
        // Добавляем компонент триггера
        TaskEventTrigger trigger = triggerGO.AddComponent<TaskEventTrigger>();
        trigger.eventCollection = selectedCollection;
        
        // Добавляем визуализацию
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(triggerGO.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 2f;
        
        // Делаем полупрозрачным
        Renderer renderer = visual.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.color = new Color(0f, 1f, 0f, 0.3f);
        renderer.material = mat;
        
        // Удаляем ненужный коллайдер с визуализации
        DestroyImmediate(visual.GetComponent<Collider>());
        
        Selection.activeObject = triggerGO;
        EditorGUIUtility.PingObject(triggerGO);
    }

    private void CreateTaskUIController()
    {
        GameObject uiGO = new GameObject("Task UI Controller");
        TaskUIController uiController = uiGO.AddComponent<TaskUIController>();
        
        selectedUIController = uiController;
        Selection.activeObject = uiGO;
        
        Debug.Log("TaskUIController created. You need to manually assign UI elements in the inspector or use the UI Setup tab.");
    }

    private void AutoFindTextElements()
    {
        if (selectedUIController == null) return;
        
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        if (allTexts.Length == 0)
        {
            Debug.LogWarning("No TextMeshProUGUI components found in scene");
            return;
        }
        
        // Показываем диалог выбора
        List<string> options = new List<string>();
        foreach (var text in allTexts)
        {
            options.Add($"{text.gameObject.name} - \"{text.text.Substring(0, Mathf.Min(30, text.text.Length))}...\"");
        }
        
        // В реальном редакторе здесь был бы более сложный UI для выбора
        Debug.Log($"Found {allTexts.Length} TextMeshProUGUI components. Please assign them manually using the UI Setup tab.");
    }

    private void SetupDefaultTaskPanel()
    {
        // Создает базовую структуру UI для задач
        if (selectedUIController == null) return;
        
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene. Please create a Canvas first.");
            return;
        }
        
        // Создаем панель задач
        GameObject panelGO = new GameObject("Task Panel");
        panelGO.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.8f);
        panelRT.anchorMax = new Vector2(0.5f, 0.8f);
        panelRT.sizeDelta = new Vector2(400, 100);
        
        // Добавляем фон
        UnityEngine.UI.Image backgroundImage = panelGO.AddComponent<UnityEngine.UI.Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.7f);
        
        // Создаем текстовый элемент
        GameObject textGO = new GameObject("Task Text");
        textGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.one * 10;
        textRT.offsetMax = Vector2.one * -10;
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = "Task text will appear here";
        textComponent.fontSize = 18;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        // Назначаем в контроллер
        selectedUIController.taskPanel = panelGO;
        selectedUIController.mainTaskText = textComponent;
        selectedUIController.taskPanelBackground = backgroundImage;
        
        EditorUtility.SetDirty(selectedUIController);
        
        Debug.Log("Default task panel created and assigned to TaskUIController");
    }

    private void ValidateUISetup()
    {
        if (selectedUIController == null)
        {
            Debug.LogError("No TaskUIController selected");
            return;
        }
        
        List<string> issues = new List<string>();
        
        if (selectedUIController.mainTaskText == null)
            issues.Add("Main task text not assigned");
            
        if (selectedUIController.taskPanel == null)
            issues.Add("Task panel not assigned");
            
        if (selectedUIController.additionalTaskTexts.Count == 0)
            issues.Add("No additional task text elements (recommended to have at least 1)");
        
        if (issues.Count == 0)
        {
            Debug.Log("✓ UI Setup validation passed!");
        }
        else
        {
            Debug.LogWarning($"UI Setup issues found:\n• {string.Join("\n• ", issues)}");
        }
    }

    private void CreateTaskSystemBootstrap()
    {
        GameObject bootstrapGO = new GameObject("Task System Bootstrap");
        TaskSystemBootstrap bootstrap = bootstrapGO.AddComponent<TaskSystemBootstrap>();
        
        bootstrap.autoCreateTaskManager = true;
        bootstrap.persistBetweenScenes = true;
        bootstrap.enableDebugLogs = true;
        
        Selection.activeObject = bootstrapGO;
        EditorGUIUtility.PingObject(bootstrapGO);
        
        Debug.Log("TaskSystemBootstrap created. This will handle TaskManager initialization and persistence.");
    }
}

/// <summary>
/// Custom Inspector для TaskEventTrigger
/// </summary>
[CustomEditor(typeof(TaskEventTrigger))]
public class TaskEventTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TaskEventTrigger trigger = (TaskEventTrigger)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Open Task System Editor"))
        {
            TaskSystemEditor.ShowWindow();
        }
        
        if (Application.isPlaying && GUILayout.Button("Force Activate Event"))
        {
            trigger.ForceActivateEvent();
        }
        
        if (GUILayout.Button("Reset Trigger"))
        {
            trigger.ResetTrigger();
        }
        
        // Показываем информацию о статусе
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Status: {trigger.GetStatusInfo()}", MessageType.Info);
    }
}

/// <summary>
/// Custom Inspector для TaskEventCollection
/// </summary>
[CustomEditor(typeof(TaskEventCollection))]
public class TaskEventCollectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TaskEventCollection collection = (TaskEventCollection)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Collection Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Open in Task System Editor"))
        {
            TaskSystemEditor window = EditorWindow.GetWindow<TaskSystemEditor>();
            // Здесь можно было бы установить выбранную коллекцию, но это требует дополнительной логики
            window.Show();
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Collection"))
        {
            var result = collection.Validate();
            result.LogResults(collection.name);
        }
        
        if (GUILayout.Button("Clean Invalid Events"))
        {
            collection.CleanInvalidEvents();
            EditorUtility.SetDirty(collection);
        }
        EditorGUILayout.EndHorizontal();
        
        // Показываем краткую статистику
        EditorGUILayout.Space();
        int validEvents = collection.events.Count(e => e.IsValid());
        EditorGUILayout.HelpBox($"Events: {validEvents} valid, {collection.events.Count - validEvents} invalid", MessageType.Info);
    }
}
#endif