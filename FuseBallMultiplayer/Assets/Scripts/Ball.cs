using System;
using System.Collections;
using DG.Tweening;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ball : NetworkBehaviour
{
    [SerializeField] float maxSpeed = 24;
    [SerializeField] float fuseDepletionAmount = 5;

    public float FuseDamage => fuseDepletionAmount;
    public float CurrentSpeed => _body.linearVelocity.magnitude;
    
    [Networked, OnChangedRender(nameof(_onActiveChanged))]
    public bool IsActive { get; set; }

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

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void SetActiveLocal(bool isActive)
    {
        _spriteRenderer.enabled = isActive;
        _collider.enabled = isActive;
        _trail.enabled = isActive;
        _body.linearVelocity = Vector2.zero;
    }

    public void ResetBall()
    {
        StartCoroutine(_resetBall());
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void UpdateBallMovementRPC(Vector3 newVel)
    {
       _body.linearVelocity = newVel;
    }
    
    IEnumerator _resetBall()
    {
        IsActive = false;

        yield return new WaitForSeconds(3);

        bool positionFound = false;

        Vector2 newBallPosition = Vector2.zero;

        while (!positionFound)
        {
            newBallPosition = new Vector2(Random.Range(-14f, 14f), Random.Range(-5f, 5f));

            var closeColliders = Physics2D.OverlapCircleAll(newBallPosition, 3);
            var playerTooClose = false;
            
            foreach (var collider in closeColliders)
            {
                var player = collider.GetComponentInParent<Player>();

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

        transform.position = newBallPosition;

        IsActive = true;
    }
    
    private void _onActiveChanged()
    {
        _spriteRenderer.enabled = IsActive;
        _collider.enabled = IsActive;
        _trail.enabled = IsActive;
        _body.linearVelocity = Vector2.zero;
    }

    /*private void OnCollisionEnter2D(Collision2D other)
    {
        var player = other.gameObject.GetComponent<Player>();

        if (player != null && player.HasStateAuthority)
        {
            _onPlayerHitRPC();
        }
    }*/
}
