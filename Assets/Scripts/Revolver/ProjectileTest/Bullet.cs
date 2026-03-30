using FishNet.Object;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 80f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float radius = 0.05f;

    private int _damage;
    private LayerMask _hitboxMask;
    private float _lifetimeTimer;
    private bool _hit;
    
    private bool _clientMoving;
    private Vector3 _clientDirection;
    
    public void Init(int damage, LayerMask hitboxMask)
    {
        _damage = damage;
        _hitboxMask = hitboxMask;
        _lifetimeTimer = lifetime;
        
        InitClientRpc(transform.position, transform.forward);
    }
    
    [ObserversRpc(BufferLast = true)]
    private void InitClientRpc(Vector3 startPos, Vector3 direction)
    {
        if (IsServerInitialized) return;

        transform.position = startPos;
        transform.forward = direction;
        _clientDirection = direction;
        _clientMoving = true;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _clientMoving = false;
    }
    
    private void Update()
    {
        if (IsServerInitialized)
            ServerUpdate();
        else
            ClientUpdate();
    }
    
    private void ServerUpdate()
    {
        if (_hit) return;

        float step = speed * Time.deltaTime;
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit,
                step, _hitboxMask, QueryTriggerInteraction.Collide))
        {
            OnHit(hit);
            return;
        }

        transform.position += direction * step;

        _lifetimeTimer -= Time.deltaTime;
        if (_lifetimeTimer <= 0f)
            NetworkObject.Despawn();
    }
    
    private void ClientUpdate()
    {
        if (!_clientMoving) return;
        transform.position += _clientDirection * (speed * Time.deltaTime);
    }

    private void OnHit(RaycastHit hit)
    {
        _hit = true;

        var hitbox = hit.collider.GetComponentInParent<Hitbox>();
        if (hitbox != null)
        {
            int finalDamage = Mathf.RoundToInt(_damage * hitbox.GetMultiplier());
            hitbox.OwnerHealth.TakeDamage(finalDamage);
        }

        NetworkObject.Despawn();
    }
}