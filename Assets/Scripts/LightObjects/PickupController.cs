using UnityEngine;
using FishNet.Object;
using FishNet.Component.Transforming;
using System.Collections;

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
        if (TryInteractDoor())
            return;
        if (TryCashIn())
            return;
        if (heldObject == null)
            TryPickup();
        else
            Throw();
    }
    private bool TryInteractDoor()
    {
        Ray ray = new Ray(cam.position, cam.forward);

        if (!Physics.Raycast(ray, out var hit, pickupDistance))
            return false;

        var door = hit.collider.GetComponentInParent<HeistDoor>();
        if (door == null) return false;

        door.ServerToggleDoor();
        return true;
    }
    private bool TryCashIn()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);

        foreach (var hit in hits)
        {
            var zone = hit.GetComponent<CashZone>();
            if (zone == null) continue;

            if (heldObject == null) return false;

            var treasure = heldObject.GetComponent<Treasure>();
            if (treasure == null) return false;

            ServerCashIn(heldObject.GetComponent<NetworkObject>());
            return true;
        }

        return false;
    }
    [ServerRpc]
    private void ServerCashIn(NetworkObject itemNetObj)
    {
        if (itemNetObj == null) return;

        var treasure = itemNetObj.GetComponent<Treasure>();
        if (treasure == null) return;

        int value = treasure.GetValue();

        CashManager.Instance.AddCash(value);

        itemNetObj.Despawn();

        ClearHeldObjectClient();
    }
    private void TryPickup()
    {
        Ray ray = new Ray(cam.position, cam.forward);
        if (!Physics.Raycast(ray, out var hit, pickupDistance, pickupLayer)) return;
        
        var revolverPickup = hit.collider.GetComponentInParent<RevolverPickup>();
        if (revolverPickup != null)
        {
            ServerTryPickupRevolver(revolverPickup.NetworkObject);
            return;
        }
        
        if (!hit.collider.TryGetComponent(out LightObject obj))
            obj = hit.collider.GetComponentInParent<LightObject>();
        if (obj == null) return;
        obj.ServerPickup(GetComponent<NetworkObject>());
    }
    
    [ServerRpc]
    private void ServerTryPickupRevolver(NetworkObject revolverNetObj)
    {
        if (revolverNetObj == null) return;
        
        RevolverPickup revolverPickup = revolverNetObj.GetComponent<RevolverPickup>();
        if (revolverPickup == null) return;
        
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;
        
        bool equipped = inventory.TryStoreRevolverAndEquip(revolverPickup, GetComponent<NetworkObject>());
        if (!equipped) return;
        
        revolverNetObj.Despawn();
    }

    public void AttachLocal(GameObject obj)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();
        heldRb.isKinematic = true;
        heldRb.useGravity = false;
        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;

        // Отключаем NetworkTransform
        var nt = obj.GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

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

        var nt = heldObject.GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = true;

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

    private void HandleSlotKey(int slot)
    {
        if (!IsOwner) return;
        Debug.Log($"[PickupController] Slot {slot} pressed, heldObject = {(heldObject != null ? heldObject.name : "null")}");

        if (heldObject != null)
        {
            ServerStoreItem(slot, heldObject.GetComponent<NetworkObject>());
        }
        else
        {
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


    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (heldObject != null)
        {
            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
            
            if (heldObject.transform.parent != holdPoint)
            {
                heldObject.transform.SetParent(holdPoint);
                heldObject.transform.localPosition = Vector3.zero;
                heldObject.transform.localRotation = Quaternion.identity;
            }
        }
    }
    public void SetHeldWeapon(GameObject weapon)
    {
        if (heldObject != null) return;

    }
}