#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Кастомный редактор для HeistTimerEventHandler с удобным редактированием текста
/// </summary>
[CustomEditor(typeof(HeistTimerEventHandler))]
public class HeistTimerEventHandlerEditor : Editor
{
    private SerializedProperty useCustomTimerEndTextProp;
    private SerializedProperty customTimerEndTextProp;
    private SerializedProperty timerEndTextColorProp;
    private SerializedProperty timerEndFontSizeProp;
    private SerializedProperty taskEventIdOnTimerEndProp;
    private SerializedProperty eventCollectionProp;

    private void OnEnable()
    {
        useCustomTimerEndTextProp = serializedObject.FindProperty("useCustomTimerEndText");
        customTimerEndTextProp = serializedObject.FindProperty("customTimerEndText");
        timerEndTextColorProp = serializedObject.FindProperty("timerEndTextColor");
        timerEndFontSizeProp = serializedObject.FindProperty("timerEndFontSize");
        taskEventIdOnTimerEndProp = serializedObject.FindProperty("taskEventIdOnTimerEnd");
        eventCollectionProp = serializedObject.FindProperty("eventCollection");
    }

    public override void OnInspectorGUI()
    {
        HeistTimerEventHandler handler = (HeistTimerEventHandler)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timer End Text Editor", EditorStyles.boldLabel);
        
        // Показываем предпросмотр текущего сообщения
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
        
        if (useCustomTimerEndTextProp.boolValue)
        {
            GUIStyle previewStyle = new GUIStyle(EditorStyles.label);
            previewStyle.normal.textColor = timerEndTextColorProp.colorValue;
            previewStyle.fontSize = timerEndFontSizeProp.intValue > 0 ? timerEndFontSizeProp.intValue : 14;
            previewStyle.wordWrap = true;
            
            EditorGUILayout.LabelField(customTimerEndTextProp.stringValue, previewStyle);
        }
        else
        {
            // Показываем предпросмотр из коллекции событий
            if (eventCollectionProp.objectReferenceValue != null)
            {
                TaskEventCollection collection = (TaskEventCollection)eventCollectionProp.objectReferenceValue;
                TaskEvent eventData = collection.GetEvent(taskEventIdOnTimerEndProp.stringValue);
                
                if (eventData != null && eventData.IsValid())
                {
                    GUIStyle previewStyle = new GUIStyle(EditorStyles.label);
                    previewStyle.normal.textColor = eventData.textColor;
                    previewStyle.fontSize = eventData.fontSize > 0 ? eventData.fontSize : 14;
                    previewStyle.wordWrap = true;
                    
                    EditorGUILayout.LabelField(eventData.taskText, previewStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("Event not found in collection", EditorStyles.centeredGreyMiniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No event collection assigned", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Быстрые пресеты
        EditorGUILayout.LabelField("Quick Presets:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Urgent Warning"))
        {
            SetCustomText("⚠️ ВРЕМЯ ВЫШЛО! НЕМЕДЛЕННО К ВЫХОДУ! ⚠️", Color.red, 24);
        }
        if (GUILayout.Button("Time's Up"))
        {
            SetCustomText("Время истекло! Поторопитесь к эвакуации!", new Color(1f, 0.5f, 0f), 20);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mission Failed"))
        {
            SetCustomText("Миссия провалена! Быстро убирайтесь!", Color.red, 22);
        }
        if (GUILayout.Button("Escape Now"))
        {
            SetCustomText("Тревога! Покиньте здание немедленно!", Color.yellow, 20);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Кнопки управления
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Timer Finished Event"))
        {
            if (Application.isPlaying)
            {
                handler.ManuallyTriggerTimerFinished();
            }
            else
            {
                EditorUtility.DisplayDialog("Test Event", 
                    "Testing is only available in Play Mode", "OK");
            }
        }
        
        if (GUILayout.Button("Reset to Default"))
        {
            SetCustomText("Время вышло! Быстро к выходу!", Color.red, 22);
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void SetCustomText(string text, Color color, int fontSize)
    {
        customTimerEndTextProp.stringValue = text;
        timerEndTextColorProp.colorValue = color;
        timerEndFontSizeProp.intValue = fontSize;
        useCustomTimerEndTextProp.boolValue = true;
        
        serializedObject.ApplyModifiedProperties();
    }
}

/// <summary>
/// Кастомный редактор для CashHUDTaskIntegration
/// </summary>
[CustomEditor(typeof(CashHUDTaskIntegration))]
public class CashHUDTaskIntegrationEditor : Editor
{
    private SerializedProperty useCustomTimerEndTextProp;
    private SerializedProperty customTimerEndTextProp;
    private SerializedProperty timerEndTextColorProp;
    private SerializedProperty timerEndFontSizeProp;

    private void OnEnable()
    {
        useCustomTimerEndTextProp = serializedObject.FindProperty("useCustomTimerEndText");
        customTimerEndTextProp = serializedObject.FindProperty("customTimerEndText");
        timerEndTextColorProp = serializedObject.FindProperty("timerEndTextColor");
        timerEndFontSizeProp = serializedObject.FindProperty("timerEndFontSize");
    }

    public override void OnInspectorGUI()
    {
        CashHUDTaskIntegration integration = (CashHUDTaskIntegration)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timer End Text Editor", EditorStyles.boldLabel);
        
        // Показываем предпросмотр
        if (useCustomTimerEndTextProp.boolValue)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
            
            GUIStyle previewStyle = new GUIStyle(EditorStyles.label);
            previewStyle.normal.textColor = timerEndTextColorProp.colorValue;
            previewStyle.fontSize = timerEndFontSizeProp.intValue > 0 ? timerEndFontSizeProp.intValue : 14;
            previewStyle.wordWrap = true;
            
            EditorGUILayout.LabelField(customTimerEndTextProp.stringValue, previewStyle);
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Быстрые пресеты
        if (useCustomTimerEndTextProp.boolValue)
        {
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Urgent"))
            {
                SetCustomText("⚠️ ВРЕМЯ ИСТЕКЛО! К ВЫХОДУ! ⚠️", Color.red, 24);
            }
            if (GUILayout.Button("Warning"))
            {
                SetCustomText("Время вышло! Поторопитесь!", new Color(1f, 0.5f, 0f), 20);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Test Timer Event") && Application.isPlaying)
        {
            integration.TestTimerFinishedEvent();
        }
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    private void SetCustomText(string text, Color color, int fontSize)
    {
        customTimerEndTextProp.stringValue = text;
        timerEndTextColorProp.colorValue = color;
        timerEndFontSizeProp.intValue = fontSize;
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif