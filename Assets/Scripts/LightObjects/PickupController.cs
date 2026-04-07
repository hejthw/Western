using UnityEngine;
using FishNet.Object;

public class PickupController : NetworkBehaviour
{
    [SerializeField] private Transform holdPoint;
    public float throwForce = 12f;
    public float pickupDistance = 5f;
    public LayerMask pickupLayer;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Transform cam;
    private PlayerInput input;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        cam = transform.Find("CameraJoint/PlayerCamera");
        if (cam == null) Debug.LogError("Camera not found!");
        if (holdPoint == null) Debug.LogError("HoldPoint NOT SET!");
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.OnPickupEvent += HandlePickup;
            input.OnAttackEvent += HandleAttack;
            input.OnSlotKeyPressed += HandleSlotKey;
            input.OnDropEvent += HandleDrop;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.OnPickupEvent -= HandlePickup;
            input.OnAttackEvent -= HandleAttack;
            input.OnSlotKeyPressed -= HandleSlotKey;
            input.OnDropEvent -= HandleDrop;
        }
    }

    private void HandleAttack()
    {
        if (!IsOwner) return;
        if (heldObject != null)
        {
            var usable = heldObject.GetComponent<IUsable>();
            usable?.Use();
        }
    }

    private void HandlePickup()
    {
        if (!IsOwner) return;
        if (heldObject == null)
            TryPickup();
        else
            Throw();
    }

    private void TryPickup()
    {
        Ray ray = new Ray(cam.position, cam.forward);
        if (!Physics.Raycast(ray, out var hit, pickupDistance, pickupLayer)) return;
        if (!hit.collider.TryGetComponent(out LightObject obj)) return;

        obj.ServerPickup(GetComponent<NetworkObject>());
    }

    public void AttachLocal(GameObject obj)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        heldRb.isKinematic = true;
        heldRb.useGravity = false;
        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        obj.transform.SetParent(holdPoint, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        Debug.Log("Attached " + obj.name + " locally");
    }

    private void Throw()
    {
        if (heldObject == null) return;
        Vector3 pos = heldObject.transform.position;
        Vector3 velocity = cam.forward * throwForce;

        heldObject.transform.SetParent(null);
        heldRb.isKinematic = false;
        heldRb.useGravity = true;
        heldRb.linearVelocity = velocity;

        heldObject.GetComponent<LightObject>().ServerThrow(pos, velocity);
        heldObject = null;
        heldRb = null;
    }

    private void HandleDrop()
    {
        if (!IsOwner) return;
        if (heldObject != null)
            Throw();
    }

    // =========================
    // INVENTORY
    // =========================

    private void HandleSlotKey(int slot)
    {
        if (!IsOwner) return;
        Debug.Log($"[PickupController] Slot {slot} pressed, heldObject = {(heldObject != null ? heldObject.name : "null")}");

        if (heldObject != null)
        {
            // Предмет в руках – сохраняем в слот
            ServerStoreItem(slot, heldObject.GetComponent<NetworkObject>());
        }
        else
        {
            // Руки пусты – пытаемся достать из слота
            ServerEquipFromSlot(slot);
        }
    }

    [ServerRpc]
    private void ServerStoreItem(int slot, NetworkObject itemNetObj)
    {
        Debug.Log($"[PickupController] ServerStoreItem slot={slot}, item={itemNetObj?.name}");
        if (itemNetObj == null) return;

        LightObject lightObj = itemNetObj.GetComponent<LightObject>();
        if (lightObj == null) return;

        PlayerInventory inv = GetComponent<PlayerInventory>();
        if (inv == null) return;

        inv.StoreItemFromHand(slot, lightObj);
        // Очищаем heldObject у всех клиентов (особенно у владельца)
        ClearHeldObjectClient();
    }

    [ServerRpc]
    private void ServerEquipFromSlot(int slot)
    {
        Debug.Log($"[PickupController] ServerEquipFromSlot slot={slot}");
        PlayerInventory inv = GetComponent<PlayerInventory>();
        if (inv == null) return;

        inv.EquipFromSlot(slot, GetComponent<NetworkObject>());
    }

    [ObserversRpc]
    private void ClearHeldObjectClient()
    {
        if (IsOwner)
        {
            heldObject = null;
            heldRb = null;
        }
    }

    public GameObject GetHeldObject() => heldObject;

    public void SetHeldWeapon(GameObject weapon)
    {
        if (heldObject != null) return;

    }
}