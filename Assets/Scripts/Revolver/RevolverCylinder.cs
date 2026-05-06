using UnityEngine;

public class RevolverCylinder : MonoBehaviour
{
    [SerializeField] private float spinDuration = 0.12f;

    private const float DegreesPerShot = 60f; // 360/6

    private float _fromAngle;
    private float _toAngle;
    private float _elapsed;
    private bool  _spinning;

    public void Spin()
    {
        if (_spinning)
            ApplyRotation(_toAngle);

        _fromAngle = _toAngle;
        _toAngle  += DegreesPerShot;
        _elapsed   = 0f;
        _spinning  = true;
    }

    private void Update()
    {
        if (!_spinning) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / spinDuration);
        
        float eased = 1f - (1f - t) * (1f - t);
        ApplyRotation(Mathf.LerpUnclamped(_fromAngle, _toAngle, eased));

        if (t >= 1f)
        {
            ApplyRotation(_toAngle);
            _spinning = false;
        }
    }

    private void ApplyRotation(float angle)
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}