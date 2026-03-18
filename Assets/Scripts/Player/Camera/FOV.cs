using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using Unity.VisualScripting;

public class FOV : MonoBehaviour
{
    private PlayerInput _input;
    [SerializeField] private CinemachineCamera cC;
    [SerializeField] private PlayerStamina _stamina;
    private Coroutine _fovCoroutine;
    
    public CameraData camData;
    
    void Awake()
    {
        _input = GetComponent<PlayerInput>();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void OnEnable()
    {
        _input.OnSprintEvent += FovChange;
        _stamina.OnStaminaEmpty += OnStaminaRanOut;
    }

    void OnDisable()
    {
        _input.OnSprintEvent -= FovChange;
        _stamina.OnStaminaEmpty -= OnStaminaRanOut;
    }
    
    private void OnStaminaRanOut()
    {
        if (_fovCoroutine != null)
            StopCoroutine(_fovCoroutine);
    
        _fovCoroutine = StartCoroutine(FovLerp(cC.Lens.FieldOfView, camData.normalFov, camData.fovLerpDuration));
    }

    private void FovChange()
    {
        bool sprintFovActive = _input.SprintHeld && !_stamina.IsEmpty;

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