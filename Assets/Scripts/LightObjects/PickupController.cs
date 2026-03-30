using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class PickupController : NetworkBehaviour
{
    [Header("Настройки")]
    public Transform holdPoint;
    public float throwForce = 12f;
    public float pickupDistance = 5f;
    public LayerMask pickupLayer;

    [Header("Инвентарь")]
    [SerializeField] private PlayerInventory inventory; 

    private GameObject heldObject;
    private LightObject lightObjectScript;

    private PlayerInput playerInput;
    private Transform cameraTransform;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        cameraTransform = transform.Find("CameraJoint/PlayerCamera")?.transform;
        if (cameraTransform == null)
            Debug.LogError("PickupController: cameraTransform не найден!");
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.OnPickupEvent += OnPickupInput;
            playerInput.OnSlotKeyPressed += OnSlotKeyPressed;
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.OnPickupEvent -= OnPickupInput;
            playerInput.OnSlotKeyPressed -= OnSlotKeyPressed;
        }
    }

    private void OnPickupInput()
    {
        if (!IsOwner) return;
        if (heldObject == null)
            TryPickup();
        else
            ThrowObject();
    }

    private void OnSlotKeyPressed(int slot)
    {
        if (!IsOwner) return;
        if (heldObject != null)
            StoreCurrentObjectToSlot(slot);
        else
            EquipFromSlot(slot);
    }

    private void TryPickup()
    {
        if (cameraTransform == null || holdPoint == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, pickupLayer)) return;

        if (hit.collider.TryGetComponent(out IPickupable pickupable))
        {
            InteractServerRpc(hit.collider.GetComponent<NetworkObject>());
            return;
        }

        if (hit.collider.TryGetComponent(out LightObject lightObj))
        {
            heldObject = hit.collider.gameObject;
            lightObjectScript = lightObj;
            heldObject.transform.SetParent(holdPoint);
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;
            lightObjectScript.OnPickup();
        }
    }

    [ServerRpc]
    private void InteractServerRpc(NetworkObject target)
    {
        if (target != null && target.TryGetComponent(out IPickupable pickupable))
            pickupable.Interact(gameObject);
    }

    private void ThrowObject()
    {
        if (heldObject == null || lightObjectScript == null) return;
        heldObject.transform.SetParent(null);
        Vector3 throwDirection = cameraTransform.forward;
        lightObjectScript.OnThrow(throwDirection * throwForce);
        heldObject = null;
        lightObjectScript = null;
    }
    public void PickupItem(GameObject item)
    {
        if (heldObject != null) return;
        heldObject = item;
        lightObjectScript = item.GetComponent<LightObject>();
        if (lightObjectScript != null)
        {
            heldObject.transform.SetParent(holdPoint);
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;
            lightObjectScript.OnPickup();
        }
    }

    private void StoreCurrentObjectToSlot(int slot)
    {
        if (heldObject == null) return;
        LightObject lightObj = heldObject.GetComponent<LightObject>();
        if (lightObj == null) return;
        NetworkObject netObj = heldObject.GetComponent<NetworkObject>();
        if (netObj == null) return;
        int prefabId = netObj.PrefabId; 
        byte[] state = null;
        if (heldObject.TryGetComponent(out ISavableItem savable))
            state = savable.SaveState();
        inventory.ServerStoreItem(slot, prefabId, state);
        NetworkObject.Despawn(netObj);
        heldObject = null;
        lightObjectScript = null;
    }

    
    private void EquipFromSlot(int slot)
    {
        if (inventory.IsSlotEmpty(slot)) return;
        inventory.ServerRemoveItem(slot);
    }
}