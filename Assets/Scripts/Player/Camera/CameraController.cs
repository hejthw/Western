using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using Unity.VisualScripting;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerPhysics pp;
    private PlayerInput _input;
    [SerializeField] private CinemachineCamera cC;
    public CameraData camData;
    
    public Transform camTransform;
    private bool shouldChangeFov = false;
    
    // headbob variables
    private float timer;
    private Vector3 _cameraOriginalPos;
    
    void Update()
    {
        if (camData.enableHeadBob) HeadBob();
    }

    void Awake()
    {
        _input = GetComponent<PlayerInput>();
    }

    void Start()
    {
        _cameraOriginalPos = camTransform.localPosition;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void OnEnable()
    {
        _input.OnSprintEvent += FovChange;
        // PlayerEvent.OnWalk.AddListener(HeadBobbing);
    }
    
    void OnDisable()
    {
        _input.OnSprintEvent -= FovChange;
        // PlayerEvent.OnWalk.RemoveListener(HeadBobbing);
    }

    private void HeadBob()
    {
        if (pp.CurrentState == PlayerState.STATE_WALK
            || pp.CurrentState == PlayerState.STATE_SPRINT)
        {
            timer += Time.deltaTime * camData.headBobSpeed;
            camTransform.localPosition = _cameraOriginalPos + new Vector3(
                Mathf.Sin(timer) * camData.headBobVector.x,
                Mathf.Sin(timer) * camData.headBobVector.y,
                Mathf.Sin(timer) * camData.headBobVector.z);
        }
        else
        {
            timer = 0;
            camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, _cameraOriginalPos, Time.deltaTime * camData.headBobSpeed);
        }
    }
    
    private void FovChange()
    {
        shouldChangeFov = (!shouldChangeFov);
        if (shouldChangeFov)
        {
            StartCoroutine(FovLerp(camData.normalFov, camData.sprintFov,camData.fovLerpDuration));
        }
        
        else
            StartCoroutine(FovLerp(camData.sprintFov, camData.normalFov, camData.fovLerpDuration));
    }
    
    private IEnumerator FovLerp(int startFov, int endFov, float lerpDuration)
    {
        float speed = 1 / lerpDuration;
    
        for (float i = 0; i < 1.0f; i += speed)
        {
            var temp = Mathf.Lerp(startFov, endFov, i);
            cC.Lens.FieldOfView = temp;
            yield return null;
        }
    }
}