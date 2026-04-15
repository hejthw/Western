using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Shoot", story: "[Self] shoot [Player] until [HasLineOfSight]", category: "Action", id: "947245aeaaa9d43db85ec4481cba68b4")]
public partial class ShootAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<bool> HasLineOfSight;

    private Transform _selfTransform;
    private Transform _playerTransform;
    private float _fireTimer;
    
    private Enforcer _enforcer;
    private NPCAttackData _data;
    private RevolverRecoilAI _recoilAI;
    private Transform _muzzleTransform;
    
    protected override Status OnStart()
    {
        if (Self?.Value == null || Player?.Value == null)
            return Status.Failure;
        
        _enforcer = Self.Value.GetComponent<Enforcer>();
        _data = _enforcer.AttackData;
        _muzzleTransform = _enforcer.RevolverMuzzle;
        _recoilAI = _enforcer.RevolverRecoilAI;
        
        _selfTransform = Self.Value.transform;
        _playerTransform = Player.Value.transform;
        _fireTimer = 0f;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!HasLineOfSight.Value)
            return Status.Failure;

        _fireTimer -= Time.deltaTime;
        if (_fireTimer <= 0f)
        {
            Shoot();
            _fireTimer = 1f / _data.FireRate;
        }

        return Status.Running;
    }

    private void Shoot()
    {
        Vector3 origin = _muzzleTransform != null 
            ? _muzzleTransform.position 
            : _selfTransform.position;
        
        Vector3 toPlayer = _playerTransform.position - origin;
        float distance = toPlayer.magnitude;

        Vector3 direction = toPlayer / distance;
        direction = ApplySpread(direction, distance);
        
        SoundBus.Play(SoundID.Shoot);
        _recoilAI.TriggerRecoil();
        
        _enforcer.ShootServerRpc(origin, direction);
        
//         if (Physics.Raycast(origin, direction, out RaycastHit hit, Mathf.Infinity, _data.HitMask, QueryTriggerInteraction.Collide))
//         {
//
//             var playerHitbox = hit.collider.GetComponentInParent<PlayerHitbox>();
//             if (playerHitbox != null)
//             {
//                 int finalDamage = Mathf.RoundToInt(_data.Damage * playerHitbox.GetMultiplier());
//                 playerHitbox.OwnerHealth.TakeDamage(finalDamage);
//             }
//
// #if UNITY_EDITOR
//             Debug.DrawLine(origin, hit.point, Color.red, 0.5f);
// #endif
//         }
// #if UNITY_EDITOR
//         else
//         {
//             Debug.DrawRay(origin, direction * 50f, Color.yellow, 0.5f);
//         }
// #endif
    }

    private Vector3 ApplySpread(Vector3 direction, float distance)
    {
        float spreadFactor = Mathf.Clamp01(distance / _data.SpreadDistance);
        float spreadAngle = spreadFactor * _data.MaxSpread;

        // Случайная точка внутри круга (не квадрата)
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle;

        float angleX = randomCircle.x * spreadAngle;
        float angleY = randomCircle.y * spreadAngle;

        // Локальные оси относительно направления выстрела
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 right = lookRotation * Vector3.right;
        Vector3 up = lookRotation * Vector3.up;

        Quaternion spreadRotation = Quaternion.AngleAxis(angleX, up) *
                                    Quaternion.AngleAxis(angleY, right);

        return spreadRotation * direction;
    }

    protected override void OnEnd()
    {
        _selfTransform = null;
        _playerTransform = null;
        _muzzleTransform = null;
        _fireTimer = 0f;
    }
}

