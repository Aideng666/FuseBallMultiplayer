using Fusion;
using UnityEngine;

public class Ball_HostMode : NetworkBehaviour
{
    [SerializeField] float maxSpeed = 24;
    [SerializeField] float fuseDepletionAmount = 5;
    
    public float FuseDamage => fuseDepletionAmount;
    public float CurrentSpeed => _body.linearVelocity.magnitude;
    
    private Rigidbody2D _body;
    private SpriteRenderer _spriteRenderer;
    private CircleCollider2D _collider;
    private TrailRenderer _trail;
    
    public override void Spawned()
    {
        _body = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<CircleCollider2D>();
        _trail = GetComponentInChildren<TrailRenderer>();
    }
    
    public override void FixedUpdateNetwork()
    {
        if (_body.linearVelocity.magnitude >= maxSpeed)
        {
            _body.linearVelocity = _body.linearVelocity.normalized * maxSpeed;
        }
    }
}
