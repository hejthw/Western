using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class FOV : MonoBehaviour
{
    [SerializeField] private PlayerInput _input;
    [SerializeField] private CinemachineCamera cC;
    
    private Coroutine _fovCoroutine;
    
    public CameraData camData;
    
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void OnEnable()
    {
        _input.OnSprintEvent += FovChange;
    }

    void OnDisable()
    {
        _input.OnSprintEvent -= FovChange;
    }

    private void FovChange()
    {
        bool sprintFovActive = _input.SprintHeld && _input.IsMoving();

        if (_fovCoroutine != null)
            StopCoroutine(_fovCoroutine);

        _fovCoroutine = sprintFovActive
            ? StartCoroutine(FovLerp(cC.Lens.FieldOfView, camData.sprintFov, camData.fovLerpDuration))
            : StartCoroutine(FovLerp(cC.Lens.FieldOfView, camData.normalFov, camData.fovLerpDuration));
    }

    private IEnumerator FovLerp(float startFov, float endFov, float lerpDuration)
    {
        float elapsed = 0f;

        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            cC.Lens.FieldOfView = Mathf.Lerp(startFov, endFov, Mathf.Clamp01(elapsed / lerpDuration));
            yield return null;
        }

        cC.Lens.FieldOfView = endFov;
    }
}