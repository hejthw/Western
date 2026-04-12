using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;

public class RevolverRecoilAI : NetworkBehaviour
{
    [Header("Recoil Settings")]
    [SerializeField] private Transform gunTransform;
    [SerializeField] private float recoilAngle = 35f;       // Угол подброса по X
    [SerializeField] private float recoilUpDuration = 0.08f; // Время подъёма
    [SerializeField] private float recoilDownDuration = 0.2f;// Время возврата

    // SyncVar — оповещает всех при изменении
    private readonly SyncVar<bool> _recoilActive = new SyncVar<bool>();

    private Quaternion _originalLocalRotation;
    private Coroutine _recoilCoroutine;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (gunTransform != null)
            _originalLocalRotation = gunTransform.localRotation;
    }
    
    public void TriggerRecoil()
    {
        if (!IsServerInitialized) return; // Только сервер вызывает

        _recoilActive.Value = !_recoilActive.Value;
        RpcOnShoot(); // Сразу оповещаем всех клиентов
    }
    
    
    [ObserversRpc(ExcludeOwner = false)]
    private void RpcOnShoot()
    {
        PlayRecoil();
    }
    
    private void PlayRecoil()
    {
        if (gunTransform == null) return;

        if (_recoilCoroutine != null)
            StopCoroutine(_recoilCoroutine);

        _recoilCoroutine = StartCoroutine(RecoilCoroutine());
    }

    private IEnumerator RecoilCoroutine()
    {
        Quaternion startRot = gunTransform.localRotation;
        Quaternion recoilRot = _originalLocalRotation *
                               Quaternion.Euler(-recoilAngle, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < recoilUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / recoilUpDuration);
            gunTransform.localRotation = Quaternion.Lerp(startRot, recoilRot, t);
            yield return null;
        }

        gunTransform.localRotation = recoilRot;
        
        elapsed = 0f;
        while (elapsed < recoilDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / recoilDownDuration);
            gunTransform.localRotation = Quaternion.Lerp(recoilRot, _originalLocalRotation, t);
            yield return null;
        }

        gunTransform.localRotation = _originalLocalRotation;
        _recoilCoroutine = null;
    }
}