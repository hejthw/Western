using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class PickupController : NetworkBehaviour
{
    [Header("Weapons")]
    [SerializeField] private NetworkObject revolverWeaponPrefab;

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
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.OnPickupEvent += OnPickupInput;
            playerInput.OnSlotKeyPressed += OnSlotKeyPressed;
            playerInput.OnAttackEvent += OnAttack;
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.OnPickupEvent -= OnPickupInput;
            playerInput.OnSlotKeyPressed -= OnSlotKeyPressed;
            playerInput.OnAttackEvent -= OnAttack;
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

    private void TryPickup()
    {
        if (cameraTransform == null || holdPoint == null || heldObject != null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, pickupLayer)) return;

        if (hit.collider.TryGetComponent(out LightObject lightObj))
        {
            lightObj.ServerPickup(GetComponent<NetworkObject>());
        }
        else if (hit.collider.TryGetComponent(out IPickupable pickupable))
        {
            InteractServerRpc(hit.collider.GetComponent<NetworkObject>());
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

        Vector3 position = heldObject.transform.position;
        Quaternion rotation = heldObject.transform.rotation;
        Vector3 force = cameraTransform.forward * throwForce;

        heldObject.transform.SetParent(null);
        lightObjectScript.ServerThrow(position, rotation, force);
        lightObjectScript.OnThrow(force); // локальное предсказание

        ClearHeldObject();
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

    public void ClearHeldObject()
    {
        heldObject = null;
        lightObjectScript = null;
    }

    private void StoreCurrentObjectToSlot(int slot)
    {
        if (heldObject == null) return;

        // Если это револьвер (оружие)
        if (heldObject.TryGetComponent<Revolver>(out Revolver revolver))
        {
            int bullets = revolver.GetBullets();
            byte[] revolverState = System.BitConverter.GetBytes(bullets);
            NetworkObject pickupPrefab = revolver.revolverPickupPrefab;
            if (pickupPrefab != null)
            {
                inventory.ServerStoreItem(slot, pickupPrefab.PrefabId, revolverState);
                NetworkObject.Despawn(heldObject.GetComponent<NetworkObject>());
                ClearHeldObject();
            }
            return;
        }

        // Обычный LightObject
        LightObject lightObj = heldObject.GetComponent<LightObject>();
        if (lightObj == null) return;

        NetworkObject netObj = heldObject.GetComponent<NetworkObject>();
        if (netObj == null) return;

        int prefabId = netObj.PrefabId;
        byte[] itemState = null;
        if (heldObject.TryGetComponent(out ISavableItem savable))
            itemState = savable.SaveState();

        inventory.ServerStoreItem(slot, prefabId, itemState);
        NetworkObject.Despawn(netObj);
        ClearHeldObject();
    }
    public void SetHeldWeapon(GameObject weapon)
    {
        if (heldObject != null) return;
        heldObject = weapon;
        lightObjectScript = null;
    }

    private void EquipFromSlot(int slot)
    {
        if (inventory.IsSlotEmpty(slot)) return;
        if (heldObject != null) return;

        int prefabId = inventory.GetItemPrefabId(slot);
        byte[] state = inventory.GetItemState(slot);

        NetworkObject prefab = NetworkManager.GetPrefab(prefabId, true);
        if (prefab != null && prefab.TryGetComponent<RevolverPickup>(out _))
        {
            Vector3 spawnPos = holdPoint.position;
            Quaternion spawnRot = holdPoint.rotation;
            NetworkObject weaponObj = Instantiate(revolverWeaponPrefab, spawnPos, spawnRot);
            NetworkManager.ServerManager.Spawn(weaponObj, Owner);

            Revolver revolver = weaponObj.GetComponent<Revolver>();
            int bullets = (state != null && state.Length >= 4)
                ? System.BitConverter.ToInt32(state, 0)
                : revolver.revolverData.bullets;

            revolver.SetBullets(bullets);
            revolver.AttachToPlayer(GetComponent<PlayerController>(), bullets);

            // Удаляем предмет из инвентаря без спавна на земле
            inventory.ServerRemoveItem(slot, false);
            return;
        }

        // Обычный LightObject – спавним на земле
        inventory.ServerRemoveItem(slot, true);
    }

    private void OnAttack()
    {
        if (!IsOwner || heldObject == null) return;
        if (heldObject.TryGetComponent<Dynamite>(out Dynamite dynamite))
            dynamite.Ignite();
    }

    private void OnSlotKeyPressed(int slot)
    {
        if (!IsOwner) return;
        if (heldObject != null)
            StoreCurrentObjectToSlot(slot);
        else
            EquipFromSlot(slot);
    }
}