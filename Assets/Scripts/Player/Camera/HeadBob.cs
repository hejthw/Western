using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using Unity.VisualScripting;

public class HeadBob: MonoBehaviour
{
    [SerializeField] private PlayerPhysics pp;
    public Transform camTransform;
    public CameraData camData;
    
    private float timer;
    private Vector3 _cameraOriginalPos;

    
    void Start()
    {
        _cameraOriginalPos = camTransform.localPosition;
    }
    
    void Update()
    {
        if (camData.enableHeadBob) Bob();
    }
    
    private void Bob()
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
    
    
}