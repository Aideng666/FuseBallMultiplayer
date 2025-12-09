using System;
using DG.Tweening;
using Fusion;
using UnityEngine;

public class Player_HostMode : NetworkBehaviour
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
    
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    [Networked] public bool IsReady { get; set; }
    [Networked] public bool GameSetupComplete { get; set; }
    [Networked] public bool GameStarted { get; set; }
    [Networked] public string NetworkedLayer { get; set; }
    [Networked] public bool TrailActive { get; set; }
    [Networked] public bool IsDead { get; set; } = false;
    [Networked] public bool StrikeVisualsActive { get; set; } = false;
    [Networked] public int KnockbackVisualDirection { get; set; }
    [Networked] public float CurrentFuseDuration { get; set; }

    public static event Action<Player_HostMode> OnPlayerSpawned;
    public static event Action<Player_HostMode> OnPlayerDespawned;
    
    public event Action<Player_HostMode> OnPlayerReadyChanged;

    private ChangeDetector _changeDetector;
    private Rigidbody2D _body;
    private Collider2D _collider;
    private TrailRenderer _dodgeTrail;
    private ParticleSystem _strikeParticles;
    private NetworkMecanimAnimator _networkAnim;
    
    private Vector3 _defaultScale;
    private float _elapsedStrikeDelay = 0;
    private float _elapsedDodgeDelay = 0;
    private float _elapsedKnockbackTime = 0;
    private float _elapsedDodgeTime = 0;
    private bool _knockbackActive = false;
    private bool _dodgeActive = false;
    private bool _isDead;

    public override void Spawned()
    {
        _body = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _dodgeTrail = GetComponentInChildren<TrailRenderer>();
        _strikeParticles = GetComponentInChildren<ParticleSystem>();
        _networkAnim = GetComponentInChildren<NetworkMecanimAnimator>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        _defaultScale = playerVisuals.transform.localScale;

        _dodgeTrail.enabled = false;
        
        OnPlayerSpawned?.Invoke(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        OnPlayerDespawned?.Invoke(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<NetworkInputData>(out var input))
        {
            return;
        }

        var pressedButtons = input.Buttons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.Buttons;

        if (GameSetupComplete && !GameStarted && pressedButtons.IsSet(NetworkInputButtons.Strike))
        {
            IsReady = !IsReady;
        }

        if (!GameStarted)
        {
            return;
        }
        
        _movePlayer(input);

        if (_canStrike() && pressedButtons.IsSet(NetworkInputButtons.Strike))
        {
            _handleStrike();
        }

        if (_canDodge() && pressedButtons.IsSet(NetworkInputButtons.Dodge))
        {
            _handleDodge();
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

    public void SetGameStarted(bool started)
    {
        GameStarted = started;
        IsDead = false;
        CurrentFuseDuration = startingFuseDuration;
    }
    
    public void ApplyKnockback(Vector2 direction, float power)
    {
        _knockbackActive = true;
        _body.linearVelocity = Vector2.zero;
        
        _body.AddForce(direction * power, ForceMode2D.Impulse);

        if (direction.x > 0)
        {
            KnockbackVisualDirection = 1;
        }
        else if (direction.x < 0)
        {
            KnockbackVisualDirection = -1;
        }
    }

    private void _movePlayer(NetworkInputData input)
    {
        var moveDirection = input.MoveDirection.normalized;

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
            StrikeVisualsActive = true;
            
            Collider2D[] collidersHit = Physics2D.OverlapCircleAll(transform.position, strikeRange);

            foreach (var collider in collidersHit)
            {
                var ball = collider.GetComponent<Ball_HostMode>();
                var player = collider.GetComponentInParent<Player_HostMode>();

                if (ball != null)
                {
                    Vector2 strikeDirection = (ball.transform.position - transform.position).normalized;
                    var velocity = strikeDirection * Mathf.Max(minStrikeSpeed, ball.GetComponent<Ball_HostMode>().CurrentSpeed);

                    /*if (powerStrike)
                    {
                        ball.GetComponent<Rigidbody2D>().linearVelocity = strikeDirection * (minStrikeSpeed * 50);
                        powerStrike = false;
                    }*/
                    
                    ball.GetComponent<Rigidbody2D>().linearVelocity = velocity;
                }

                if (player != null)
                {
                    if (player == this)
                    {
                        continue;
                    }
                    
                    Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                    player.ApplyKnockback(knockbackDirection, strikeKnockbackPower);
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
    
    private void _modifyFuseDuration(float amount)
    {
        CurrentFuseDuration += amount;
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsReady):

                    OnPlayerReadyChanged?.Invoke(this);

                    break;

                case nameof(NetworkedLayer):

                    gameObject.layer = LayerMask.NameToLayer(NetworkedLayer);
                    _collider.gameObject.layer = LayerMask.NameToLayer(NetworkedLayer);

                    break;

                case nameof(TrailActive):

                    _dodgeTrail.enabled = TrailActive;

                    break;

                case nameof(IsDead):

                    if (IsDead)
                    {
                        _networkAnim.SetTrigger("Death");
                    }

                    break;

                case nameof(StrikeVisualsActive):

                    if (StrikeVisualsActive)
                    {
                        print("Playing Strike Visual");
                        playerVisuals.transform.DOKill();
                        playerVisuals.transform.localScale = _defaultScale;

                        playerVisuals.transform.DOPunchScale(new Vector3(0.75f, 0.75f, 0.75f), 0.4f, 1);
                        _strikeParticles.Play();

                        StrikeVisualsActive = false;
                    }

                    break;

                case nameof(KnockbackVisualDirection):

                    if (KnockbackVisualDirection == 0)
                    {
                        return;
                    }
                    
                    playerVisuals.transform.DOKill();
                    playerVisuals.transform.rotation = Quaternion.identity;

                    if (KnockbackVisualDirection > 0)
                    {
                        playerVisuals.transform.DOPunchRotation(new Vector3(0, 0, -60), knockbackStunLength, 4).OnComplete(
                            () =>
                            {
                                KnockbackVisualDirection = 0;
                            });
                    }
                    else if (KnockbackVisualDirection < 0)
                    {
                        playerVisuals.transform.DOPunchRotation(new Vector3(0, 0, 60), knockbackStunLength, 4).OnComplete(
                            () =>
                            {
                                KnockbackVisualDirection = 0;
                            });
                    }
                    
                    break;
            }
        }
    }
}
