using System.Collections;
using DG.Tweening;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class Ball_HostMode : NetworkBehaviour
{
    [SerializeField] float maxSpeed = 24;
    [SerializeField] float fuseDepletionAmount = 5;
    
    [Networked] public bool IsActive { get; set; }
    [Networked] public bool ResetBallPosition { get; set; }
    
    public float FuseDamage => fuseDepletionAmount;
    public float CurrentSpeed => _body.linearVelocity.magnitude;
    
    private Rigidbody2D _body;
    private NetworkRigidbody2D _networkBody;
    private SpriteRenderer _spriteRenderer;
    private CircleCollider2D _collider;
    private TrailRenderer _trail;
    private ChangeDetector _changeDetector;
    private Vector3 _startingPosition;

    private bool _ballResetting = false;
    
    public override void Spawned()
    {
        _body = GetComponent<Rigidbody2D>();
        _networkBody = GetComponent<NetworkRigidbody2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _collider = GetComponent<CircleCollider2D>();
        _trail = GetComponentInChildren<TrailRenderer>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        IsActive = true;

        _startingPosition = transform.position;
    }
    
    public override void FixedUpdateNetwork()
    {
        if (ResetBallPosition)
        {
            _body.linearVelocity = Vector2.zero;
            _networkBody.Teleport(_startingPosition, Quaternion.identity);
            ResetBallPosition = false;
        }
        
        if (_body.linearVelocity.magnitude >= maxSpeed)
        {
            _body.linearVelocity = _body.linearVelocity.normalized * maxSpeed;
        }
    }
    
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsActive):
                    
                    _spriteRenderer.enabled = IsActive;
                    _collider.enabled = IsActive;
                    _trail.enabled = IsActive;
                    _body.linearVelocity = Vector2.zero;

                    break;
            }
        }
    }
    
    public void ResetBall()
    {
        StartCoroutine(_resetBall());
    }
    
    IEnumerator _resetBall()
    {
        _ballResetting = true;

        bool positionFound = false;
        Vector2 newBallPosition = Vector2.zero;

        while (!positionFound)
        {
            newBallPosition = new Vector2(Random.Range(-14f, 14f), Random.Range(-5f, 5f));

            var closeColliders = Physics2D.OverlapCircleAll(newBallPosition, 3);
            var playerTooClose = false;
            
            foreach (var collider in closeColliders)
            {
                var player = collider.GetComponentInParent<Player_HostMode>();

                if (player != null)
                {
                    playerTooClose = true;
                    break;
                }
            }

            if (!playerTooClose)
            {
                positionFound = true;
            }

            yield return null;
        }
        
        _networkBody.Teleport(newBallPosition, Quaternion.identity);
        
        yield return new WaitForSeconds(3);

        IsActive = true;
        _ballResetting = false;
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        var player = other.gameObject.GetComponent<Player_HostMode>();

        if (player != null && !_ballResetting)
        {
            Camera.main.transform.DOShakePosition(0.4f, 1f, 8, 90);
            player.ModifyFuseDuration(-FuseDamage);
            IsActive = false;
            StartCoroutine(_resetBall());
        }
    }
}
