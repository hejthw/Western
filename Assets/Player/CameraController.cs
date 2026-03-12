using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using Unity.VisualScripting;

public class CameraController : MonoBehaviour
{
    #region Variables: Dependecies
    [Header("Dependecies")]
    [SerializeField] private PlayerPhysics pp;
    private PlayerInput _input;
    [SerializeField] private CinemachineCamera cC;
    #endregion
    
    #region Variables: FOV
    [Header("FOV")]
    public int normalFov = 80;
    public int sprintFov = 85;
    public float fovLerpDuration = 50f;
    
    private bool shouldChangeFov = false;
    #endregion
    
    #region Variables: HeadBob
    [Header("HeadBob")]
    public bool enableHeadBob = true;
    public Vector3 headBobVector = new Vector3(0.001f, 0.08f, 0.01f);
    public Transform camTransform;
    public float headBobSpeed = 5f;
    
    private float timer;
    private Vector3 _cameraOriginalPos;
    #endregion
    
    void Update()
    {
        if (enableHeadBob) HeadBob();
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
            timer += Time.deltaTime * headBobSpeed;
            camTransform.localPosition = _cameraOriginalPos + new Vector3(
                Mathf.Sin(timer) * headBobVector.x,
                Mathf.Sin(timer) * headBobVector.y,
                Mathf.Sin(timer) * headBobVector.z);
        }
        else
        {
            timer = 0;
            camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, _cameraOriginalPos, Time.deltaTime * headBobSpeed);
        }
    }
    
    private void FovChange()
    {
        shouldChangeFov = (!shouldChangeFov);
        if (shouldChangeFov)
        {
            StartCoroutine(FovLerp(normalFov, sprintFov,fovLerpDuration));
        }
        
        else
            StartCoroutine(FovLerp(sprintFov, normalFov, fovLerpDuration));
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