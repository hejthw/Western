using UnityEngine;
using System.Collections.Generic;

public class TutorialUISpawner : MonoBehaviour
{
    [System.Serializable]
    public struct TutorialEntry
    {
        public string tag;
        public GameObject prefab;
    }

    [Header("References")]
    [SerializeField] private InteractionScanner scanner;
    [SerializeField] private PickupController pickupController;
    [SerializeField] private Transform container;

    [Header("Mapping")]
    [SerializeField] private TutorialEntry[] entries;

    [Header("Common Prefabs")]
    [SerializeField] private GameObject pickupHintPrefab;
    [SerializeField] private GameObject throwHintPrefab;
    [SerializeField] private GameObject useHintPrefab;
    [SerializeField] private GameObject throwCashZonePrefab;
    [SerializeField] private GameObject dropRevolverPrefab;

    private readonly List<GameObject> _spawnedInstances = new();
    private readonly Dictionary<string, GameObject> _map = new();
    private GameObject _currentTarget;

    public bool _isInCashZone;
    public bool _isHolding;
    private bool _isRevolverHolding;

    private void Awake()
    {
        foreach (var entry in entries)
            if (!string.IsNullOrEmpty(entry.tag) && entry.prefab != null)
                _map[entry.tag] = entry.prefab;
    }

    private void OnEnable()
    {
        scanner.OnFocusChanged += HandleFocusChanged;
        UIEvents.OnCashZoneChanged += HandleCashZoneChanged;
    }

    private void OnDisable()
    {
        scanner.OnFocusChanged -= HandleFocusChanged;
        UIEvents.OnCashZoneChanged -= HandleCashZoneChanged;
    }
    
    private void HandleCashZoneChanged(bool state)
    {
        _isInCashZone = state;

        if (_isInCashZone && _isHolding)
        {
            ClearAll();
            Spawn(throwCashZonePrefab);
        }
        else if (_isHolding)
        {
            ClearAll();
            Spawn(throwHintPrefab);
        }
    }

    // Вызывать из PickupController когда игрок поднял предмет
    public void OnItemPickedUp(GameObject item)
    {
        _isHolding = true;
        ClearAll();
        Spawn(throwHintPrefab);
        
        if (item.GetComponent<IUsable>() != null)
            Spawn(useHintPrefab);
    }

    public void OnRevolverPickedUp()
    {
        _isRevolverHolding = true;
        ClearAll();

        Spawn(dropRevolverPrefab);
    }
    
    public void OnRevolverDroppedUp()
    {
        _isRevolverHolding = false;
        ClearAll();
        
        if (_currentTarget != null)
            HandleFocusChanged(_currentTarget);
    }

    // Вызывать из PickupController когда игрок бросил/выбросил предмет
    public void OnItemDropped()
    {
        _isHolding = false;
        ClearAll();

        // Если после броска всё ещё смотрим на интерактивный объект — показываем снова
        if (_currentTarget != null)
            HandleFocusChanged(_currentTarget);
    }

    private void HandleFocusChanged(GameObject target)
    {
        _currentTarget = target;

        // Если сейчас держим предмет — фокус не меняет UI
        if (pickupController.GetHeldObject() != null || _isRevolverHolding) return;

        ClearAll();

        if (target == null) return;

        if (_map.TryGetValue(target.tag, out GameObject prefab))
            Spawn(prefab);

        Spawn(pickupHintPrefab);
    }
    

    private void Spawn(GameObject prefab)
    {
        if (prefab == null) return;
        Debug.Log($"Spawning {prefab.name}");
        var instance = Instantiate(prefab, container);
        _spawnedInstances.Add(instance);
    }

    private void ClearAll()
    {
        foreach (var instance in _spawnedInstances)
            if (instance != null) Destroy(instance);
        _spawnedInstances.Clear();
    }
}