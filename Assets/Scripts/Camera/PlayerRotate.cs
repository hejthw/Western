using System.Collections;
using FishNet.Object;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerRotate : NetworkBehaviour
{
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private Transform transform;
    private bool _interuptUpdate;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            enabled = false;
    }
    
    private void OnEnable()
    {
        PlayerEffectsEvents.OnThrowup += EndDrunk;
    }
    
    private void OnDisable()
    {
        PlayerEffectsEvents.OnThrowup -= EndDrunk;
    }
    
    void Update()
    {
        if (!_interuptUpdate) Rotate();
    }

    void Rotate()
    {
        float yaw = panTilt.PanAxis.Value;
        float pitch = panTilt.TiltAxis.Value;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void EndDrunk()
    {
        panTilt.TiltAxis.Value = 70f;
        SoundBus.Play(SoundID.PlayerHurt);
        StartCoroutine(WaitForEndDrunk());
    }

    private IEnumerator WaitForEndDrunk()
    {
        _interuptUpdate = true;
        yield return new WaitForSeconds(3f);
        _interuptUpdate = false;
    }
}