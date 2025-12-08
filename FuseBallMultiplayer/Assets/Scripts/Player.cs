using System;
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
    [SerializeField] private float startingFuseDuration;
    [SerializeField] private float dodgeFuseDepletionAmount;
    
    public static event Action<Player> OnPlayerJoined;
    public event Action<bool> OnPlayerReady;
    
    [Networked, OnChangedRender(nameof(_onLayerChanged))]
    public string NetworkedLayer { get; set; }
    
    [Networked, OnChangedRender(nameof(_onTrailChanged))]
    public bool TrailActive { get; set; }

    [Networked, OnChangedRender(nameof(_onDeadChanged))]
    public bool IsDead { get; set; } = false;
    
    [Networked, OnChangedRender(nameof(_onReadyChanged))]
    public bool IsReady { get; set; } = false;
    
    [Networked, OnChangedRender(nameof(_onFuseChanged))]
    public float CurrentFuseDuration { get; set; }
    
    [Networked]
    public NetworkButtons ButtonsPrevious { get; set; }
    
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
    private bool _gameSetupComplete = false;
    private float _currentFuseDuration;
    
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
        if (!HasStateAuthority || IsDead)
        {
            return;
        }
        
        if (!GetInput<NetworkInputData>(out var input))
        {
            return;
        }
        
        var pressedButtons = input.Buttons.GetPressed(ButtonsPrevious);

        //print($"Setup Complete: {_gameSetupComplete} Game Started: {_gameStarted} Button Pressed: {pressedButtons.IsSet(NetworkInputButtons.Strike)}");
        
        if (_gameSetupComplete && pressedButtons.IsSet(NetworkInputButtons.Strike) && !_gameStarted)
        {
            IsReady = !IsReady;
        }
        
        ButtonsPrevious = input.Buttons;
        
        if (!_gameStarted)
        {
            return;
        }

        if (!_knockbackActive && !_dodgeActive)
        {
            _movePlayer(input);

            if (_canStrike() && pressedButtons.IsSet(NetworkInputButtons.Strike))
            {
                _handleStrike();
            }

            if (_canDodge() && pressedButtons.IsSet(NetworkInputButtons.Dodge))
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

        if (CurrentFuseDuration <= 0)
        {
            IsDead = true;
        }

        CurrentFuseDuration -= Runner.DeltaTime;
    }


    private void _movePlayer(NetworkInputData input)
    {
        var moveDirection = input.MoveDirection.normalized;

        /*if (input.Buttons.IsSet(NetworkInputButtons.Up))
        {
            moveDirection.y += 1;
        }
        if (input.Buttons.IsSet(NetworkInputButtons.Down))
        {
            moveDirection.y -= 1;
        }
        if (input.Buttons.IsSet(NetworkInputButtons.Left))
        {
            moveDirection.x -= 1;
        }
        if (input.Buttons.IsSet(NetworkInputButtons.Right))
        {
            moveDirection.x += 1;
        }*/

        //moveDirection = moveDirection.normalized;

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
    
    private void _handleStrike()
    {
        if (_canStrike())
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

                    if (ball.HasStateAuthority)
                    {
                       ball.GetComponent<Rigidbody2D>().linearVelocity = strikeDirection * Mathf.Max(minStrikeSpeed, ball.GetComponent<Ball>().CurrentSpeed);
                    }
                    else
                    {
                        ball.UpdateBallMovementRPC(strikeDirection * Mathf.Max(minStrikeSpeed, ball.GetComponent<Ball>().CurrentSpeed));
                    }
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
        if (_canDodge())
        {
            _dodgeActive = true;
            TrailActive = true;

            NetworkedLayer = "Intangible";
            
            Vector2 dodgeDirection = (_body.linearVelocity).normalized;

            if (dodgeDirection.magnitude == 0)
            {
                dodgeDirection = new Vector2(0, -1);
            }
            
            _modifyFuseDuration(-dodgeFuseDepletionAmount);
            _body.AddForce(dodgeDirection * dodgeForce, ForceMode2D.Impulse);
            
            _elapsedDodgeDelay = 0;
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
        IsDead = false;
        
        CurrentFuseDuration = startingFuseDuration;
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void SetGameSetupCompleteRPC(bool setupComplete)
    {
        _gameSetupComplete = setupComplete;
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void _onBallHitPlayerRPC(Ball ball)
    {
        print("RPC Reset");
        ball.ResetBall();
    }

    private void _onBallHitPlayerLocal(Ball ball)
    {
        print("Local Reset");
        ball.SetActiveLocal(false);
        ball.ResetBall();
    }
    
    private void _modifyFuseDuration(float amount)
    {
        CurrentFuseDuration += amount;
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
    
    private void _onDeadChanged()
    {
        if (IsDead)
        {
            _networkAnim.SetTrigger("Death");
        }
    }

    private void _onReadyChanged()
    {
        if (Runner.IsSharedModeMasterClient)
        {
            OnPlayerReady?.Invoke(IsReady);
        }
    }
    
    private void _onFuseChanged()
    {
        //TODO: UI Updates here?
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
            Camera.main.transform.DOShakePosition(0.4f, 1f, 8, 90);
            _modifyFuseDuration(-ball.FuseDamage);
            _onBallHitPlayerLocal(ball);
            _onBallHitPlayerRPC(ball);
        }
    }

    public void ApplyPowerup()
    {
        //TODO: Apply Powerups
    }
}
