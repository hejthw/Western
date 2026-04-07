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

        if (cam == null)
        {
            Debug.LogError("Camera not found!");
        }

        if (holdPoint == null)
        {
            Debug.LogError("HoldPoint NOT SET!");
        }
    }

    private void OnEnable()
    {
        if (input != null)
            input.OnPickupEvent += HandlePickup;
    }

    private void OnDisable()
    {
        if (input != null)
            input.OnPickupEvent -= HandlePickup;
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

        if (!Physics.Raycast(ray, out var hit, pickupDistance, pickupLayer))
            return;

        if (!hit.collider.TryGetComponent(out LightObject obj))
            return;

   obj.ServerPickup(GetComponent<NetworkObject>());

        AttachLocal(obj.gameObject);
    }

    private void AttachLocal(GameObject obj)
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
        Debug.Log("Kinematic: " + heldRb.isKinematic);
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

        heldObject.GetComponent<LightObject>()
            .ServerThrow(pos, velocity);

        heldObject = null;
        heldRb = null;
    }
    public void SetHeldWeapon(GameObject weapon)
    {
        if (heldObject != null) return;

    }
    private void LateUpdate()
    {
        if (!IsOwner) return;

        if (heldObject != null)
        {
            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
        }
    }

}
