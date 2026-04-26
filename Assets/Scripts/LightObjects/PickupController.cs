using UnityEngine;
using FishNet.Object;
using FishNet.Component.Transforming;
using System.Collections;
using FishNet.Connection;

public class PickupController : NetworkBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private LootDatabase lootDatabase;
    public float throwForce = 12f;
    public float pickupDistance = 5f;
    public LayerMask pickupLayer;

    private GameObject heldObject;
    private Rigidbody heldRb;
    private Transform cam;
    private PlayerInput input;
    private bool _isInsideCashZone;
    private bool _lastLoggedCashZoneState;
  
    [SerializeField] private TutorialUISpawner uiSpawner;

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
        if (TryInteractRope())
            return;
        if (TryCashIn())
            return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.IsArmed)
        {
            TrySwitchFromRevolverToLookedItem();
            return;
        }

        if (heldObject != null)
        {
            Revolver revolver = heldObject.GetComponent<Revolver>();
            if (revolver != null)
            {
                Debug.Log("Cannot pick up while holding a revolver");
                return;
            }
            else
            {
                Throw();
            }
        }
        TryPickup();
    }

    private void TrySwitchFromRevolverToLookedItem()
    {
        Ray ray = new Ray(cam.position, cam.forward);
        if (!Physics.Raycast(ray, out var hit, pickupDistance, pickupLayer))
            return;

        if (!hit.collider.TryGetComponent(out LightObject obj))
            obj = hit.collider.GetComponentInParent<LightObject>();
        if (obj == null)
            return;

        ServerSwitchFromRevolverToItem(obj.NetworkObject);
    }

    [ServerRpc]
    private void ServerSwitchFromRevolverToItem(NetworkObject targetItemNetObj)
    {
        if (targetItemNetObj == null)
            return;

        PlayerInventory inv = GetComponent<PlayerInventory>();
        if (inv == null)
            return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc == null || !pc.IsArmed)
            return;

        NetworkObject playerNetObj = GetComponent<NetworkObject>();
        inv.UnequipRevolver(playerNetObj);

        LightObject targetItem = targetItemNetObj.GetComponent<LightObject>();
        if (targetItem == null)
            return;

        targetItem.ServerPickup(playerNetObj);
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

    private bool TryInteractRope()
    {
        Ray ray = new Ray(cam.position, cam.forward);
        if (!Physics.Raycast(ray, out var hit, pickupDistance))
            return false;

        var rope = hit.collider.GetComponentInParent<ClimbRopeNetwork>();
        if (rope == null || !rope.IsRopeActive)
            return false;

        ServerTryRopeTeleport(rope.NetworkObject);
        return true;
    }

    [ServerRpc]
    private void ServerTryRopeTeleport(NetworkObject ropeNetObj)
    {
        if (ropeNetObj == null) return;
        ClimbRopeNetwork rope = ropeNetObj.GetComponent<ClimbRopeNetwork>();
        if (rope == null) return;
        rope.ServerTryTeleportToTop(GetComponent<NetworkObject>());
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

        NetworkObject playerNetObj = GetComponent<NetworkObject>();
        int slot = inventory.TryStoreRevolverPickupInSlot(revolverPickup, playerNetObj);
        if (slot < 0) return;

        revolverNetObj.Despawn();
        inventory.EquipFromSlot(slot, playerNetObj);
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
        
        // Принудительная синхронизация игрока после физического взаимодействия
        ForcePlayerPositionSync();
        
        uiSpawner?.OnItemPickedUp(obj);
        Debug.Log("Attached " + obj.name + " locally");
    }

    public void Throw()
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
        
        // Принудительная синхронизация игрока после физического взаимодействия
        ForcePlayerPositionSync();
        
        heldObject = null;
        heldRb = null;
        uiSpawner?.OnItemDropped();
    }

    private void HandleDrop()
    {
        if (!IsOwner) return;
        if (heldObject != null)
        {
            Revolver revolver = heldObject.GetComponent<Revolver>();
            if (revolver != null)
            {
                // Для револьвера дроп по кнопке отключён.
                return;
            }
            else
            {
                Throw();
            }
        }
    }

    private void HandleSlotKey(int slot)
    {
        if (!IsOwner) return;
        Debug.Log($"[PickupController] Slot {slot} pressed, heldObject = {(heldObject != null ? heldObject.name : "null")}");

        if (heldObject != null)
        {
            // Для любого предмета (обычного или револьвера) вызываем обмен/сохранение
            ServerProcessItemSlot(slot, heldObject.GetComponent<NetworkObject>());
        }
        else
        {
            // Руки пусты – экипируем из слота
            ServerEquipFromSlot(slot);
        }
    }
    [ServerRpc]
    private void ServerTryUnequipRevolver(int pressedSlot)
    {
        PlayerController pc = GetComponent<PlayerController>();
        if (pc == null) return;

        Revolver revolver = pc.GetCurrentWeapon();
        if (revolver == null) return;

        if (revolver.GetBoundSlot() == pressedSlot)
        {
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.UnequipRevolver(GetComponent<NetworkObject>());
            }
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
    public bool IsInsideCashZone() => _isInsideCashZone;

    public string GetInteractBindingLabel()
    {
        return input != null ? input.GetPickupBindingDisplay() : "E";
    }

    public bool TryGetHeldLootValue(out int value)
    {
        value = 0;
        if (heldObject == null)
            return false;

        Treasure treasure = heldObject.GetComponent<Treasure>();
        if (treasure == null)
            return false;

        value = ResolveLootValue(treasure, heldObject.name);
        return value > 0;
    }
    
    [TargetRpc]
    public void TargetSetCashZone(NetworkConnection conn, bool entered)
    {
        UIEvents.RaiseOnCashZoneChanged(entered);
    }


    private void LateUpdate()
    {
        if (!IsOwner) return;
        _isInsideCashZone = CheckCashZoneNearby();
        if (_isInsideCashZone != _lastLoggedCashZoneState)
        {
            Debug.Log($"[PickupController] Local cash zone changed: {_isInsideCashZone}, player={name}");
            _lastLoggedCashZoneState = _isInsideCashZone;
        }
        if (heldObject != null)
        {
            // Оружие не трогаем – оно само управляет своей позицией
            if (heldObject.GetComponent<IWeapon>() != null)
                return;

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
        heldObject = weapon;
        heldRb = weapon.GetComponent<Rigidbody>();
        if (heldRb != null)
        {
            heldRb.isKinematic = true;
            heldRb.useGravity = false;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
        }
        var nt = weapon.GetComponent<NetworkTransform>();
        if (nt != null) nt.enabled = false;

        // Если это оружие (реализует IWeapon) – не меняем родителя (уже прикреплён через AttachClientRpc)
        if (weapon.GetComponent<IWeapon>() != null)
        {
            Debug.Log($"SetHeldWeapon: {weapon.name} is weapon, skip reparenting");
            return;
        }

        // Обычный предмет – прикрепляем к holdPoint
        weapon.transform.SetParent(holdPoint, false);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        Debug.Log($"SetHeldWeapon: {weapon.name} attached to holdPoint");
    }

    public void ClearHeld()
    {
        heldObject = null;
        heldRb = null;
        Debug.Log("[PickupController] ClearHeld called");
    }
    [ServerRpc]
    private void ServerProcessItemSlot(int slot, NetworkObject itemNetObj)
    {
        if (itemNetObj == null) return;
        PlayerInventory inv = GetComponent<PlayerInventory>();
        if (inv == null) return;

        Revolver revolver = itemNetObj.GetComponent<Revolver>();
        if (revolver != null)
        {
            int currentSlot = revolver.GetBoundSlot();
            if (currentSlot == slot)
            {
                inv.UnequipRevolver(GetComponent<NetworkObject>());
            }
            else
            {
                if (!inv.IsSlotEmpty(slot))
                {
                    NetworkObject playerNetObj = GetComponent<NetworkObject>();
                    // Убираем текущий предмет в его исходный слот и достаем предмет из нажатого слота.
                    inv.UnequipRevolver(playerNetObj);
                    inv.EquipFromSlot(slot, playerNetObj);
                }
                else
                {
                    inv.MoveBoundRevolverToSlot(revolver, slot);
                }
            }
            return;
        }

        LightObject lightObj = itemNetObj.GetComponent<LightObject>();
        if (lightObj == null) return;

        if (inv.IsSlotEmpty(slot))
        {
            inv.StoreItemFromHand(slot, lightObj);
            ClearHeldObjectClient();
            return;
        }

        int targetPrefabId = inv.GetItemPrefabId(slot);
        NetworkObject targetPrefab = NetworkManager.GetPrefab(targetPrefabId, true);
        bool isTargetRevolver = targetPrefab != null && targetPrefab.GetComponent<RevolverPickup>() != null;

        if (isTargetRevolver)
        {
            return;
        }

        byte[] oldState = inv.GetItemState(slot);
        int oldPrefabId = targetPrefabId;
        inv.ClearSlot(slot);
        inv.StoreItemFromHand(slot, lightObj);
        ClearHeldObjectClient();

        NetworkObject oldPrefab = NetworkManager.GetPrefab(oldPrefabId, true);
        if (oldPrefab != null)
        {
            NetworkObject spawned = Instantiate(oldPrefab, transform.position, Quaternion.identity);
            NetworkManager.ServerManager.Spawn(spawned, Owner);
            LightObject oldLightObj = spawned.GetComponent<LightObject>();
            if (oldLightObj != null)
            {
                oldLightObj.DeserializeState(oldState);
                oldLightObj.EquipToPlayer(GetComponent<NetworkObject>());
            }
        }
    }

    private bool CheckCashZoneNearby()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponent<CashZone>() != null ||
                hits[i].GetComponentInParent<CashZone>() != null ||
                hits[i].GetComponentInChildren<CashZone>() != null)
                return true;
        }

        return false;
    }

    private int ResolveLootValue(Treasure treasure, string heldName)
    {
        if (lootDatabase != null && lootDatabase.items != null)
        {
            string normalizedName = NormalizeObjectName(heldName);
            for (int i = 0; i < lootDatabase.items.Length; i++)
            {
                TreasureData data = lootDatabase.items[i];
                if (data == null || data.prefab == null)
                    continue;

                if (NormalizeObjectName(data.prefab.name) == normalizedName)
                    return data.value;
            }
        }

        return treasure.GetValue();
    }

    private string NormalizeObjectName(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
            return string.Empty;

        const string cloneSuffix = "(Clone)";
        string trimmed = sourceName.Trim();
        if (trimmed.EndsWith(cloneSuffix))
            return trimmed.Substring(0, trimmed.Length - cloneSuffix.Length).Trim();

        return trimmed;
    }

    /// <summary>
    /// Принудительная синхронизация позиции игрока после физических взаимодействий
    /// </summary>
    private void ForcePlayerPositionSync()
    {
        if (!IsOwner) return;

        // Получаем компоненты игрока
        PlayerController playerController = GetComponent<PlayerController>();
        PlayerPhysics playerPhysics = GetComponent<PlayerPhysics>();
        NetworkTransform nt = GetComponent<NetworkTransform>();
        Rigidbody rb = GetComponent<Rigidbody>();

        // Сбрасываем физику и предсказания
        if (playerPhysics != null)
        {
            playerPhysics.ClearStaleMotionAfterNetworkSnap();
            playerPhysics.ResetOwnerMovementPredictionAfterForcedMove();
        }

        // Принудительно синхронизируем NetworkTransform
        if (nt != null && nt.enabled)
        {
            try
            {
                nt.Teleport();
                nt.ForceSend();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PickupController] ForcePlayerPositionSync failed: {ex.Message}");
            }
        }

        // Сбрасываем физические силы
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Синхронизация через PlayerController если необходимо
        if (playerController != null)
        {
            playerController.NotifyNetworkTransformHardSync();
        }
    }
}