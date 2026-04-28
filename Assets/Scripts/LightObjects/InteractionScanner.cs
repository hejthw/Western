using UnityEngine;
using FishNet.Object;

public class InteractionScanner : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scanDistance = 5f;
    [SerializeField] private LayerMask scanLayers;
    [SerializeField] private string interactableTag = "Interactable";
    [SerializeField] private string doorTag = "Door";

    private Transform _cam;
    private GameObject _current;

    public GameObject Current => _current;

    public event System.Action<GameObject> OnFocusChanged;

    private void Start()
    {
        _cam = transform.Find("PlayerCamera");
        if (_cam == null) Debug.LogError("[InteractionScanner] Camera not found!");
    }

    private void Update()
    {
        if (!IsOwner) return;

        GameObject found = Scan();

        if (found == _current) return;

        _current = found;
        OnFocusChanged?.Invoke(_current);

        Debug.Log(_current != null
            ? $"[InteractionScanner] Focused: {_current.name}"
            : "[InteractionScanner] No interactable in sight");
    }

    private GameObject Scan()
    {
        if (_cam == null) return null;

        Ray ray = new Ray(_cam.position, _cam.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, scanDistance, scanLayers))
            return null;

        GameObject obj = hit.collider.gameObject;

        if (obj.CompareTag(interactableTag))
        {
            return obj;
        }

        if (obj.CompareTag(doorTag))
        {
            return obj;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_cam == null) return;
        Gizmos.color = _current != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(_cam.position, _cam.forward * scanDistance);
    }
}