using System;
using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameObject playerVisuals;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float minStrikeSpeed;
    [SerializeField] private float strikeKnockbackPower;
    [SerializeField] private float strikeDelay;
    [SerializeField] private float strikeRange;
    [SerializeField] private float dodgeDelay;
    [SerializeField] private float knockbackStunLength;
    [SerializeField] private float dodgeDuration;
    [SerializeField] private float dodgeForce;
    [SerializeField] private float fuseDuration;
    [SerializeField] private float dodgeFuseDepletionAmount;
    
    public static event Action<Player> OnPlayerJoined;
    
    [Networked, OnChangedRender(nameof(_onLayerChanged))]
    public string NetworkedLayer { get; set; }
    
    [Networked, OnChangedRender(nameof(_onTrailChanged))]
    public bool TrailActive { get; set; }
    
    private Rigidbody2D _body;
    private Collider2D _collider;
    private TrailRenderer _dodgeTrail;
    private ParticleSystem _strikeParticles;
    private NetworkMecanimAnimator _networkAnim;
    
    private float _elapsedStrikeDelay = 0;
    private float _elapsedDodgeDelay = 0;
    private float _elapsedKnockbackTime = 0;
    private float _elapsedDodgeTime = 0;
    private Vector3 _defaultScale;

    private bool _knockbackActive = false;
    private bool _dodgeActive = false;
    private bool _isDead;
    private bool _gameStarted = false;
    
    public override void Spawned()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponentInChildren<Collider2D>();
        _dodgeTrail = GetComponentInChildren<TrailRenderer>();
        _networkAnim = GetComponentInChildren<NetworkMecanimAnimator>();
        _strikeParticles = GetComponentInChildren<ParticleSystem>();

        _defaultScale = playerVisuals.transform.localScale;

        _dodgeTrail.enabled = false;
        
        OnPlayerJoined?.Invoke(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_gameStarted)
        {
            return;
        }
        
        InputSystem.Update();

        if (!_knockbackActive && !_dodgeActive)
        {
            _movePlayer();

            if (_canStrike())
            {
                _handleStrike();
            }

            if (_canDodge())
            {
                _handleDodge();
            }
        }

        if (_elapsedStrikeDelay < strikeDelay)
        {
            _elapsedStrikeDelay += Runner.DeltaTime;
        }
        
        if (_elapsedDodgeDelay < dodgeDelay)
        {
            _elapsedDodgeDelay += Runner.DeltaTime;
        }

        if (_knockbackActive && _elapsedKnockbackTime < knockbackStunLength)
        {
            _elapsedKnockbackTime += Runner.DeltaTime;

            if (_elapsedKnockbackTime >= knockbackStunLength)
            {
                _knockbackActive = false;
                _elapsedKnockbackTime = 0;
            }
        }
        
        if (_dodgeActive && _elapsedDodgeTime < dodgeDuration)
        {
            _elapsedDodgeTime += Runner.DeltaTime;

            if (_elapsedDodgeTime >= dodgeDuration)
            {
                _dodgeActive = false;
                _elapsedDodgeTime = 0;
                NetworkedLayer = "Player";
                TrailActive = false;
            }
        }
    }
    

    private void _movePlayer()
    {
        var moveDirection = InputManager.Instance.GetMoveInput().normalized;

        _body.linearVelocity = moveDirection * moveSpeed;
        
        if (_body.linearVelocity.x > 0)
        {
            _networkAnim.Animator.SetBool("Right", true);
            _networkAnim.Animator.SetBool("Left", false);
            _networkAnim.Animator.SetBool("Up", false);
            _networkAnim.Animator.SetBool("Down", false);
        }
        else if (_body.linearVelocity.x < 0)
        {
            _networkAnim.Animator.SetBool("Right", false);
            _networkAnim.Animator.SetBool("Left", true);
            _networkAnim.Animator.SetBool("Up", false);
            _networkAnim.Animator.SetBool("Down", false);
        }
        else if (_body.linearVelocity.y > 0)
        {
            _networkAnim.Animator.SetBool("Right", false);
            _networkAnim.Animator.SetBool("Left", false);
            _networkAnim.Animator.SetBool("Up", true);
            _networkAnim.Animator.SetBool("Down", false);
        }
        else if (_body.linearVelocity.y < 0)
        {
            _networkAnim.Animator.SetBool("Right", false);
            _networkAnim.Animator.SetBool("Left", false);
            _networkAnim.Animator.SetBool("Up", false);
            _networkAnim.Animator.SetBool("Down", true);
        }
        else if (_body.linearVelocity.magnitude == 0)
        {
            _networkAnim.Animator.SetBool("Right", false);
            _networkAnim.Animator.SetBool("Left", false);
            _networkAnim.Animator.SetBool("Up", false);
            _networkAnim.Animator.SetBool("Down", false);
        }
    }
    
    public void ApplyKnockbackLocal(Vector2 direction, float power)
    {
        _knockbackActive = true;
        _body.linearVelocity = Vector2.zero;
        
        _body.AddForce(direction * power, ForceMode2D.Impulse);
        _playKnockbackVisualsLocal(direction.x);
    }

    private void _playKnockbackVisualsLocal(float xVelocity)
    {
        playerVisuals.transform.DOKill();
        playerVisuals.transform.rotation = Quaternion.identity;
        
        if (xVelocity > 0)
        {
            playerVisuals.transform.DOPunchRotation(new Vector3(0, 0, -60), knockbackStunLength, 4);
        }
        else if (xVelocity < 0)
        {
            playerVisuals.transform.DOPunchRotation(new Vector3(0, 0, 60), knockbackStunLength, 4);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void SetGameStartedRPC(bool started)
    {
        _gameStarted = started;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void SetStartPositionRPC(Vector3 pos)
    {
        transform.position = pos;
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void ApplyKnockbackRPC(Vector2 direction, float power)
    {
        if (!HasStateAuthority)
        {
            return;
        }
        
        ApplyKnockbackLocal(direction, power);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void _showStrikeVisualsRPC()
    {
        playerVisuals.transform.DOKill();
        playerVisuals.transform.localScale = _defaultScale;
        
        playerVisuals.transform.DOPunchScale(new Vector3(0.75f, 0.75f, 0.75f), 0.4f, 1);
        _strikeParticles.Play();
    }
    
    /*[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void _showKnockbackVisualsRPC(float xVelocity)
    {
        playerVisuals.transform.DOKill();
        playerVisuals.transform.rotation = Quaternion.identity;
        
        if (xVelocity > 0)
        {
            playerVisuals.transform.DOPunchRotation(new Vector3(0, 0, -60), knockbackStunLength, 4);
        }
        else if (xVelocity < 0)
        {
            playerVisuals.transform.DOPunchRotation(new Vector3(0, 0, 60), knockbackStunLength, 4);
        }
    }*/
    
    private void _modifyFuseDuration(float amount)
    {
        fuseDuration += amount;
    }

    private void _handleStrike()
    {
        if (InputManager.Instance.StrikePressed() && _canStrike())
        {
            _showStrikeVisualsRPC();
            
            Collider2D[] collidersHit = Physics2D.OverlapCircleAll(transform.position, strikeRange);

            foreach (var collider in collidersHit)
            {
                var ball = collider.GetComponent<Ball>();
                var player = collider.GetComponentInParent<Player>();

                if (ball != null)
                {
                    Vector2 strikeDirection = (ball.transform.position - transform.position).normalized;

                    /*if (powerStrike)
                    {
                        ball.GetComponent<Rigidbody2D>().linearVelocity = strikeDirection * (minStrikeSpeed * 50);
                        powerStrike = false;
                    }*/
                    
                    ball.GetComponent<Rigidbody2D>().linearVelocity = strikeDirection * Mathf.Max(minStrikeSpeed, ball.GetComponent<Ball>().CurrentSpeed);
                }

                if (player != null)
                {
                    if (player == this)
                    {
                        continue;
                    }
                    
                    Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                    player.ApplyKnockbackLocal(knockbackDirection, strikeKnockbackPower);
                    player.ApplyKnockbackRPC(knockbackDirection, strikeKnockbackPower);
                }
            }

            _elapsedStrikeDelay = 0;
        }
    }

    private void _handleDodge()
    {
        if (InputManager.Instance.DodgePressed() && _canDodge())
        {
            _dodgeActive = true;
            TrailActive = true;

            NetworkedLayer = "Intangible";
            
            Vector2 dodgeDirection = (_body.linearVelocity).normalized;

            if (dodgeDirection.magnitude == 0)
            {
                dodgeDirection = new Vector2(0, -1);
            }
            
            _body.AddForce(dodgeDirection * dodgeForce, ForceMode2D.Impulse);
            
            _elapsedDodgeDelay = 0;
        }
    }

    private void _onLayerChanged()
    {
        gameObject.layer = LayerMask.NameToLayer(NetworkedLayer);
        _collider.gameObject.layer = LayerMask.NameToLayer(NetworkedLayer);
    }
    
    private void _onTrailChanged()
    {
        _dodgeTrail.enabled = TrailActive;
    }
    
    private bool _canStrike()
    {
        if (_elapsedStrikeDelay >= strikeDelay)
        {
            return true;
        }

        return false;
    }

    private bool _canDodge()
    {
        if (_elapsedDodgeDelay >= dodgeDelay)
        {
            return true;
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!HasStateAuthority)
        {
            return;
        }

        var ball = other.gameObject.GetComponent<Ball>();

        if (ball != null)
        {
            _modifyFuseDuration(-ball.FuseDamage);
        }
    }

    public void ApplyPowerup()
    {
        //TODO: Apply Powerups
    }
}
