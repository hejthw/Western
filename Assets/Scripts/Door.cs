using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class Door : NetworkBehaviour
{
    [SerializeField] private Transform doorVisual;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;

    private readonly SyncVar<bool> isOpen = new SyncVar<bool>();

    private Quaternion closedRot;
    private Quaternion openRot;

    private void Start()
    {
        closedRot = doorVisual.localRotation;
        openRot = Quaternion.Euler(0, openAngle, 0) * closedRot;

        isOpen.OnChange += OnDoorStateChanged;
    }

    private void OnDestroy()
    {
        isOpen.OnChange -= OnDoorStateChanged;
    }

    private void Update()
    {
        Quaternion target = isOpen.Value ? openRot : closedRot;

        doorVisual.localRotation = Quaternion.Lerp(
            doorVisual.localRotation,
            target,
            Time.deltaTime * openSpeed
        );
    }

    private void OnDoorStateChanged(bool oldVal, bool newVal, bool asServer)
    {
        Debug.Log($"Door state: {newVal}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerToggleDoor()
    {
        isOpen.Value = !isOpen.Value;
    }
}
