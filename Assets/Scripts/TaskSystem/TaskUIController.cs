using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Контроллер UI для отображения задач игрокам
/// </summary>
public class TaskUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Главный элемент интерфейса для отображения текущей задачи")]
    public TextMeshProUGUI mainTaskText;
    
    [Tooltip("Дополнительные текстовые элементы для множественных задач")]
    public List<TextMeshProUGUI> additionalTaskTexts = new List<TextMeshProUGUI>();
    
    [Tooltip("Панель задач (для анимации появления/скрытия)")]
    public GameObject taskPanel;
    
    [Tooltip("Фоновое изображение панели задач")]
    public Image taskPanelBackground;
    
    [Header("Auto-Find Settings")]
    [Tooltip("Автоматически искать UI элементы по путям")]
    public bool autoFindUIElements = true;
    
    [Tooltip("Путь к главному тексту задачи в иерархии префаба игрока")]
    public string mainTaskTextPath = "player-ui-currenttask-task";
    
    [Tooltip("Искать UI элементы только у владельца")]
    public bool onlyForOwner = true;
    
    [Header("Animation Settings")]
    [Tooltip("Использовать анимацию появления/скрытия")]
    public bool useAnimation = true;
    
    [Tooltip("Длительность анимации появления")]
    public float fadeInDuration = 0.5f;
    
    [Tooltip("Длительность анимации скрытия")]
    public float fadeOutDuration = 0.3f;
    
    [Tooltip("Тип анимации")]
    public TaskAnimationType animationType = TaskAnimationType.Fade;
    
    [Header("Display Settings")]
    [Tooltip("Максимальное количество одновременно отображаемых задач")]
    [Range(1, 5)]
    public int maxDisplayedTasks = 3;
    
    [Tooltip("Автоматически скрывать панель когда нет задач")]
    public bool autoHideWhenEmpty = true;
    
    [Tooltip("Время отображения задачи по умолчанию (0 = бесконечно)")]
    public float defaultDisplayTime = 0f;
    
    [Tooltip("Сохранять оригинальный стиль текста (шрифт, размер, цвет)")]
    public bool preserveOriginalStyle = true;
    
    [Tooltip("Новая задача заменяет старую вместо добавления")]
    public bool replaceOldTasks = true;
    
    [Header("Visual Settings")]
    [Tooltip("Цвет фона панели по умолчанию")]
    public Color defaultBackgroundColor = new Color(0, 0, 0, 0.7f);
    
    [Tooltip("Размер шрифта по умолчанию")]
    public float defaultFontSize = 18f;
    
    [Tooltip("Префикс для нумерации задач")]
    public string taskNumberPrefix = "• ";
    
    [Header("Audio")]
    [Tooltip("Звук появления новой задачи")]
    public AudioClip taskAppearSound;
    
    [Tooltip("Звук завершения задачи")]
    public AudioClip taskCompleteSound;
    
    [Tooltip("Громкость звуков")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("Debug")]
    [Tooltip("Показывать отладочные сообщения")]
    public bool enableDebugLogs = false;

    // Внутренние переменные
    private readonly Dictionary<string, TaskDisplayInfo> activeTasks = new Dictionary<string, TaskDisplayInfo>();
    private readonly Queue<TaskEvent> taskQueue = new Queue<TaskEvent>();
    private readonly List<TextMeshProUGUI> allTextElements = new List<TextMeshProUGUI>();
    
    private CanvasGroup panelCanvasGroup;
    private RectTransform panelRectTransform;
    private bool isAnimating = false;

    // События для подписки
    public System.Action<TaskEvent> OnTaskShown;
    public System.Action<string> OnTaskHidden;

    private void Awake()
    {
        // Проверяем, находимся ли мы на префабе игрока
        PlayerController playerController = GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            TaskUIController[] playerControllers = playerController.GetComponentsInChildren<TaskUIController>(true);
            if (playerControllers.Length > 0 && playerControllers[0] != this)
            {
                enabled = false;
                return;
            }

            // На игроке UI должен искаться по локальной копии владельца
            onlyForOwner = true;
            StartCoroutine(ApplyOwnerTaskUIVisibility(playerController));
        }

        InitializeUI();
        
        // Если нужно автопоиск, делаем это при старте
        if (autoFindUIElements)
        {
            // Ждем, пока игроки спавнятся, затем ищем UI
            StartCoroutine(DelayedUISearch());
        }
        else
        {
            SetupTextElements();
        }
        
        // Скрываем панель в начале если нужно
        if (autoHideWhenEmpty && taskPanel != null)
        {
            taskPanel.SetActive(false);
        }
    }

    private void InitializeUI()
    {
        // Настраиваем CanvasGroup для анимации
        if (taskPanel != null)
        {
            panelCanvasGroup = taskPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = taskPanel.AddComponent<CanvasGroup>();
                
            panelRectTransform = taskPanel.GetComponent<RectTransform>();
        }
        
        // Настраиваем фон панели
        if (taskPanelBackground != null)
        {
            taskPanelBackground.color = defaultBackgroundColor;
        }
    }

    private void SetupTextElements()
    {
        allTextElements.Clear();
        
        // Добавляем основной элемент
        if (mainTaskText != null)
        {
            allTextElements.Add(mainTaskText);
            SetupTextElement(mainTaskText);
        }
        
        // Добавляем дополнительные элементы
        foreach (var textElement in additionalTaskTexts)
        {
            if (textElement != null)
            {
                allTextElements.Add(textElement);
                SetupTextElement(textElement);
            }
        }
        
        // Скрываем все элементы в начале
        foreach (var textElement in allTextElements)
        {
            textElement.gameObject.SetActive(false);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[TaskUIController] Initialized {allTextElements.Count} text elements");
    }

    private void SetupTextElement(TextMeshProUGUI textElement)
    {
        if (textElement.fontSize == 0 || textElement.fontSize < 10)
            textElement.fontSize = defaultFontSize;
    }

    private IEnumerator ApplyOwnerTaskUIVisibility(PlayerController playerController)
    {
        float timeout = 2f;
        while (timeout > 0f && playerController != null && !playerController.IsSpawned)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (playerController == null || playerController.IsOwner)
            yield break;

        if (taskPanel != null)
            taskPanel.SetActive(false);

        if (mainTaskText != null)
            mainTaskText.gameObject.SetActive(false);

        foreach (var textElement in additionalTaskTexts)
        {
            if (textElement != null)
                textElement.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Показать новую задачу
    /// </summary>
    public void ShowTask(TaskEvent taskEvent)
    {
        // Если контроллер находится на игроке, показываем только владельцу
        PlayerController playerController = GetComponentInParent<PlayerController>();
        if (playerController != null && !playerController.IsOwner)
        {
            return;
        }

        if (taskEvent == null || !taskEvent.IsValid())
        {
            Debug.LogError("[TaskUIController] Invalid task event provided");
            return;
        }
        
        
        // Проверяем, не отображается ли уже эта задача
        if (activeTasks.ContainsKey(taskEvent.eventId))
        {
            if (enableDebugLogs)
                Debug.Log($"[TaskUIController] Task '{taskEvent.eventId}' is already displayed");
            return;
        }
        
        // Если включена замена старых задач, очищаем все активные
        if (replaceOldTasks && activeTasks.Count > 0)
        {
            ClearAllTasks();
            
            if (enableDebugLogs)
                Debug.Log($"[TaskUIController] Cleared old tasks to show new task '{taskEvent.eventId}'");
        }
        
        // Если достигнут лимит задач и не включена замена, добавляем в очередь
        if (!replaceOldTasks && activeTasks.Count >= maxDisplayedTasks)
        {
            taskQueue.Enqueue(taskEvent);
            if (enableDebugLogs)
                Debug.Log($"[TaskUIController] Task '{taskEvent.eventId}' queued (max tasks reached)");
            return;
        }
        
        DisplayTask(taskEvent);
    }

    private void DisplayTask(TaskEvent taskEvent)
    {
        // Находим свободный элемент текста
        TextMeshProUGUI targetText = GetAvailableTextElement();
        if (targetText == null)
        {
            Debug.LogError($"[TaskUIController] No available text elements for displaying task '{taskEvent.eventId}'. " +
                          $"Active tasks: {activeTasks.Count}, Max tasks: {maxDisplayedTasks}, " +
                          $"Main text: {(mainTaskText != null ? "present" : "missing")}, " +
                          $"Additional texts: {additionalTaskTexts.Count}");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TaskUIController] Displaying task '{taskEvent.eventId}' on element '{targetText.name}'. " +
                     $"Active tasks: {activeTasks.Count + 1}/{maxDisplayedTasks}");
        }
        
        // Создаем информацию об отображении
        TaskDisplayInfo displayInfo = new TaskDisplayInfo
        {
            taskEvent = taskEvent,
            textElement = targetText,
            startTime = Time.time,
            displayDuration = taskEvent.autoHideAfter > 0 ? taskEvent.autoHideAfter : defaultDisplayTime
        };
        
        // Применяем настройки к элементу текста
        taskEvent.ApplyToTextComponent(targetText, preserveOriginalStyle);
        
        // Добавляем префикс если нужно
        if (!string.IsNullOrEmpty(taskNumberPrefix))
        {
            targetText.text = taskNumberPrefix + targetText.text;
        }
        
        // Регистрируем задачу
        activeTasks[taskEvent.eventId] = displayInfo;
        
        // Показываем элемент
        targetText.gameObject.SetActive(true);
        
        // Показываем панель если она была скрыта
        if (taskPanel != null && !taskPanel.activeSelf)
        {
            ShowPanel();
        }
        
        // Анимация появления
        if (useAnimation && !isAnimating)
        {
            StartCoroutine(AnimateTaskAppearance(displayInfo));
        }
        
        // Воспроизводим звук
        if (taskAppearSound != null)
        {
            AudioSource.PlayClipAtPoint(taskAppearSound, Camera.main.transform.position, soundVolume);
        }
        
        // Запускаем автоскрытие если нужно
        if (displayInfo.displayDuration > 0)
        {
            StartCoroutine(AutoHideTask(taskEvent.eventId, displayInfo.displayDuration));
        }
        
        // Уведомляем подписчиков
        OnTaskShown?.Invoke(taskEvent);
        
        if (enableDebugLogs)
            Debug.Log($"[TaskUIController] Displayed task: '{taskEvent.eventId}' - {taskEvent.taskText}");
    }

    private TextMeshProUGUI GetAvailableTextElement()
    {
        // При замене старых задач всегда используем основной элемент
        if (replaceOldTasks && mainTaskText != null)
        {
            return mainTaskText;
        }
        
        // Проверяем основной элемент - если он не используется активными задачами
        if (mainTaskText != null && !IsTextElementUsed(mainTaskText))
            return mainTaskText;
        
        // Затем ищем среди дополнительных
        foreach (var textElement in additionalTaskTexts)
        {
            if (textElement != null && !IsTextElementUsed(textElement))
                return textElement;
        }
        
        // Если нет свободных элементов, используем основной (перезаписываем)
        if (mainTaskText != null)
        {
            if (enableDebugLogs)
                Debug.Log("[TaskUIController] All text elements busy, reusing main text element");
            return mainTaskText;
        }
        
        return null;
    }

    /// <summary>
    /// Проверяет, используется ли текстовый элемент активными задачами
    /// </summary>
    private bool IsTextElementUsed(TextMeshProUGUI textElement)
    {
        foreach (var task in activeTasks.Values)
        {
            if (task.textElement == textElement)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Скрыть задачу по ID
    /// </summary>
    public void HideTask(string eventId)
    {
        if (!activeTasks.TryGetValue(eventId, out TaskDisplayInfo displayInfo))
        {
            if (enableDebugLogs)
                Debug.Log($"[TaskUIController] Task '{eventId}' not found for hiding");
            return;
        }
        
        // Анимация скрытия
        if (useAnimation && !isAnimating)
        {
            StartCoroutine(AnimateTaskDisappearance(displayInfo));
        }
        else
        {
            CompleteTaskHiding(displayInfo);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[TaskUIController] Hiding task: '{eventId}'");
    }

    private void CompleteTaskHiding(TaskDisplayInfo displayInfo)
    {
        // Удаляем из активных задач
        activeTasks.Remove(displayInfo.taskEvent.eventId);
        
        // В режиме замены просто очищаем текст, элемент остается активным для следующей задачи
        if (replaceOldTasks)
        {
            displayInfo.textElement.text = "";
        }
        else
        {
            // В режиме множественных задач скрываем элемент
            displayInfo.textElement.gameObject.SetActive(false);
            displayInfo.textElement.text = "";
        }
        
        // Воспроизводим звук завершения
        if (taskCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(taskCompleteSound, Camera.main.transform.position, soundVolume);
        }
        
        // Уведомляем подписчиков
        OnTaskHidden?.Invoke(displayInfo.taskEvent.eventId);
        
        // Показываем следующую задачу из очереди
        ShowNextQueuedTask();
        
        // Скрываем панель если больше нет задач
        if (activeTasks.Count == 0 && autoHideWhenEmpty)
        {
            HidePanel();
        }
    }

    /// <summary>
    /// Очистить все задачи
    /// </summary>
    public void ClearAllTasks()
    {
        foreach (var displayInfo in activeTasks.Values)
        {
            displayInfo.textElement.gameObject.SetActive(false);
        }
        
        activeTasks.Clear();
        taskQueue.Clear();
        
        if (autoHideWhenEmpty)
        {
            HidePanel();
        }
        
        if (enableDebugLogs)
            Debug.Log("[TaskUIController] All tasks cleared");
    }

    private void ShowNextQueuedTask()
    {
        if (taskQueue.Count > 0 && activeTasks.Count < maxDisplayedTasks)
        {
            TaskEvent nextTask = taskQueue.Dequeue();
            DisplayTask(nextTask);
        }
    }

    private void ShowPanel()
    {
        if (taskPanel != null)
        {
            taskPanel.SetActive(true);
            
            if (useAnimation)
            {
                StartCoroutine(AnimatePanelShow());
            }
        }
    }

    private void HidePanel()
    {
        if (taskPanel != null)
        {
            if (useAnimation)
            {
                StartCoroutine(AnimatePanelHide());
            }
            else
            {
                taskPanel.SetActive(false);
            }
        }
    }

    private IEnumerator AnimateTaskAppearance(TaskDisplayInfo displayInfo)
    {
        isAnimating = true;
        
        switch (animationType)
        {
            case TaskAnimationType.Fade:
                yield return AnimateFade(displayInfo.textElement, 0f, 1f, fadeInDuration);
                break;
                
            case TaskAnimationType.Scale:
                yield return AnimateScale(displayInfo.textElement, Vector3.zero, Vector3.one, fadeInDuration);
                break;
                
            case TaskAnimationType.Slide:
                yield return AnimateSlide(displayInfo.textElement, true, fadeInDuration);
                break;
        }
        
        isAnimating = false;
    }

    private IEnumerator AnimateTaskDisappearance(TaskDisplayInfo displayInfo)
    {
        isAnimating = true;
        
        switch (animationType)
        {
            case TaskAnimationType.Fade:
                yield return AnimateFade(displayInfo.textElement, 1f, 0f, fadeOutDuration);
                break;
                
            case TaskAnimationType.Scale:
                yield return AnimateScale(displayInfo.textElement, Vector3.one, Vector3.zero, fadeOutDuration);
                break;
                
            case TaskAnimationType.Slide:
                yield return AnimateSlide(displayInfo.textElement, false, fadeOutDuration);
                break;
        }
        
        CompleteTaskHiding(displayInfo);
        isAnimating = false;
    }

    private IEnumerator AnimateFade(TextMeshProUGUI textElement, float fromAlpha, float toAlpha, float duration)
    {
        Color color = textElement.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            color.a = alpha;
            textElement.color = color;
            yield return null;
        }
        
        color.a = toAlpha;
        textElement.color = color;
    }

    private IEnumerator AnimateScale(TextMeshProUGUI textElement, Vector3 fromScale, Vector3 toScale, float duration)
    {
        Transform transform = textElement.transform;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(fromScale, toScale, elapsed / duration);
            yield return null;
        }
        
        transform.localScale = toScale;
    }

    private IEnumerator AnimateSlide(TextMeshProUGUI textElement, bool slideIn, float duration)
    {
        RectTransform rectTransform = textElement.GetComponent<RectTransform>();
        Vector2 originalPosition = rectTransform.anchoredPosition;
        Vector2 offscreenPosition = originalPosition + Vector2.left * 1000f; // Слева за экраном
        
        Vector2 startPos = slideIn ? offscreenPosition : originalPosition;
        Vector2 endPos = slideIn ? originalPosition : offscreenPosition;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPos;
    }

    private IEnumerator AnimatePanelShow()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            
            panelCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator AnimatePanelHide()
    {
        if (panelCanvasGroup != null)
        {
            float elapsed = 0f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            
            panelCanvasGroup.alpha = 0f;
        }
        
        taskPanel.SetActive(false);
    }

    private IEnumerator AutoHideTask(string eventId, float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTask(eventId);
    }

    /// <summary>
    /// Получить количество активных задач
    /// </summary>
    public int GetActiveTaskCount()
    {
        return activeTasks.Count;
    }

    /// <summary>
    /// Получить количество задач в очереди
    /// </summary>
    public int GetQueuedTaskCount()
    {
        return taskQueue.Count;
    }

    /// <summary>
    /// Проверить, отображается ли задача
    /// </summary>
    public bool IsTaskDisplayed(string eventId)
    {
        return activeTasks.ContainsKey(eventId);
    }

    /// <summary>
    /// Тестовый метод для отображения задачи
    /// </summary>
    [ContextMenu("Test Show Sample Task")]
    public void TestShowSampleTask()
    {
        TaskEvent testTask = new TaskEvent
        {
            eventId = "test_task_" + Time.time,
            eventName = "Test Task",
            taskText = "This is a test task for UI testing",
            textColor = Color.yellow,
            fontSize = 20,
            autoHideAfter = 3f
        };
        
        ShowTask(testTask);
    }

    [ContextMenu("Test Clear All Tasks")]
    public void TestClearAllTasks()
    {
        ClearAllTasks();
    }

    /// <summary>
    /// Корутина для отложенного поиска UI элементов
    /// </summary>
    private System.Collections.IEnumerator DelayedUISearch()
    {
        // Ждем несколько кадров, пока игроки точно спавнятся
        yield return new WaitForSeconds(1f);
        
        AutoFindUIElements();
        SetupTextElements();
    }

    /// <summary>
    /// Автоматический поиск UI элементов на префабах игроков
    /// </summary>
    public void AutoFindUIElements()
    {
        if (string.IsNullOrEmpty(mainTaskTextPath))
        {
            if (enableDebugLogs)
                Debug.LogWarning("[TaskUIController] Main task text path is not set");
            return;
        }

        // Ищем всех игроков в сцене
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        
        if (players.Length == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[TaskUIController] No players found in scene for UI search");
            return;
        }

        PlayerController targetPlayer = null;

        if (onlyForOwner)
        {
            // Ищем только владельца (локального игрока)
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    targetPlayer = player;
                    break;
                }
            }
        }
        else
        {
            // Берем первого игрока
            targetPlayer = players[0];
        }

        if (targetPlayer == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[TaskUIController] No target player found for UI search");
            return;
        }

        // Ищем UI элемент по пути
        Transform taskTextTransform = FindChildByPath(targetPlayer.transform, mainTaskTextPath);
        
        if (taskTextTransform != null)
        {
            TextMeshProUGUI foundText = taskTextTransform.GetComponent<TextMeshProUGUI>();
            if (foundText != null)
            {
                mainTaskText = foundText;
                
                // Также найти панель (родительский объект текста)
                if (taskPanel == null)
                {
                    taskPanel = foundText.transform.parent?.gameObject;
                }

                if (enableDebugLogs)
                    Debug.Log($"[TaskUIController] Found main task text: {foundText.name} on player {targetPlayer.name}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[TaskUIController] Found object at path '{mainTaskTextPath}' but it doesn't have TextMeshProUGUI component");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[TaskUIController] Could not find UI element at path '{mainTaskTextPath}' on player {targetPlayer.name}");
        }
    }

    /// <summary>
    /// Поиск дочернего объекта по пути в иерархии
    /// </summary>
    private Transform FindChildByPath(Transform parent, string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        string[] pathParts = path.Split('/');
        Transform current = parent;

        foreach (string part in pathParts)
        {
            Transform child = current.Find(part);
            if (child == null)
            {
                // Пробуем найти среди всех детей (рекурсивно)
                child = FindChildRecursive(current, part);
            }
            
            if (child == null)
                return null;
                
            current = child;
        }

        return current;
    }

    /// <summary>
    /// Рекурсивный поиск дочернего объекта по имени
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            
            if (child.name == childName)
                return child;
                
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        
        return null;
    }

    /// <summary>
    /// Принудительно обновить поиск UI элементов
    /// </summary>
    [ContextMenu("Refresh UI Elements")]
    public void RefreshUIElements()
    {
        if (autoFindUIElements)
        {
            AutoFindUIElements();
            SetupTextElements();
            
            if (enableDebugLogs)
                Debug.Log("[TaskUIController] UI elements refreshed");
        }
    }

    /// <summary>
    /// Создает дополнительный текстовый элемент на основе основного
    /// </summary>
    private TextMeshProUGUI CreateAdditionalTextElement()
    {
        if (mainTaskText == null) return null;

        // Создаем копию основного текстового элемента
        GameObject newTextGO = Instantiate(mainTaskText.gameObject, mainTaskText.transform.parent);
        newTextGO.name = $"Task Text Additional {additionalTaskTexts.Count + 1}";
        
        TextMeshProUGUI newTextComp = newTextGO.GetComponent<TextMeshProUGUI>();
        
        // Позиционируем под предыдущим элементом
        RectTransform newRect = newTextGO.GetComponent<RectTransform>();
        RectTransform mainRect = mainTaskText.GetComponent<RectTransform>();
        
        if (newRect != null && mainRect != null)
        {
            newRect.anchoredPosition = mainRect.anchoredPosition + Vector2.down * (mainRect.sizeDelta.y + 10) * (additionalTaskTexts.Count + 1);
        }
        
        // Изначально скрываем
        newTextGO.SetActive(false);
        
        return newTextComp;
    }
}

/// <summary>
/// Информация об отображаемой задаче
/// </summary>
public class TaskDisplayInfo
{
    public TaskEvent taskEvent;
    public TextMeshProUGUI textElement;
    public float startTime;
    public float displayDuration;
}

/// <summary>
/// Типы анимации для задач
/// </summary>
public enum TaskAnimationType
{
    None,
    Fade,
    Scale,
    Slide
}