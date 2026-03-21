using UnityEngine;
using FishNet.Object;

public class PickupController : NetworkBehaviour
{
    [Header("Настройки")]
    public Transform holdPoint;
    public float throwForce = 12f;
    public float pickupDistance = 5f;
    public LayerMask pickupLayer;

    private GameObject heldObject;
    private LightObject lightObjectScript;

    private PlayerInput playerInput;
    private Transform cameraTransform;       

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        cameraTransform = transform.Find("CameraJoint/PlayerCamera")?.transform;

        if (cameraTransform == null)
            Debug.LogError("PickupController: cameraTransform не найден! Проверь иерархию игрока.");
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.OnPickupEvent += OnPickupInput;
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.OnPickupEvent -= OnPickupInput;
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
        if (cameraTransform == null || holdPoint == null)
        {
            Debug.LogError("PickupController: cameraTransform или holdPoint не назначены!");
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, pickupLayer))
        {
            Debug.Log("Raycast ничего не попал (проверь дистанцию и LayerMask)");
            return;
        }

        // Сначала проверяем IPickupable — приоритет выше
        if (hit.collider.TryGetComponent(out IPickupable pickupable))
        {
            InteractServerRpc(hit.collider.GetComponent<NetworkObject>());
            return;
        }

        // Обычный лёгкий предмет
        if (hit.collider.TryGetComponent(out LightObject lightObj))
        {
            heldObject = hit.collider.gameObject;
            lightObjectScript = lightObj;

            heldObject.transform.SetParent(holdPoint);
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;

            lightObjectScript.OnPickup();
            Debug.Log("Предмет поднят успешно!");
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
}