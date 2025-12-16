using System;
using Fusion;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class Powerup : NetworkBehaviour
{
    [SerializeField] private Sprite speedSprite;
    [SerializeField] private Sprite strengthSprite;
    [SerializeField] private Sprite timeSprite;

    public event Action OnPowerupPickedUp; 
    
    [Networked] public PowerupType PowerupType { get; set; }

    private SpriteRenderer _spriteRenderer;
    private ChangeDetector _changeDetector;
    private PowerupType _powerupType;
    private NetworkMecanimAnimator _networkAnim;

    public override void Spawned()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _networkAnim = GetComponent<NetworkMecanimAnimator>();
        
        _spriteRenderer.transform.DOScale(new Vector3(1.25f, 1.25f, 1.25f), 2)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        if (Runner.IsServer)
        {
            var powerupType = Random.Range(1, 4);
            PowerupType = (PowerupType)powerupType;
        }
        
        _setPowerup(PowerupType);
    }

    private void _setPowerup(PowerupType type)
    {
        _powerupType = type;
        
        switch (type)
        {
            case PowerupType.Strength:

                _spriteRenderer.sprite = strengthSprite;
                
                break;
            case PowerupType.Speed:
                
                _spriteRenderer.sprite = speedSprite;
                
                break;
            case PowerupType.Time:
                
                _spriteRenderer.sprite = timeSprite;
                
                break;
        }
    }
    
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PowerupType):
                    
                    _setPowerup(PowerupType);

                    break;
            }
        }
    }

    private void _onPowerupPickedUp()
    {
        OnPowerupPickedUp?.Invoke();
        _networkAnim.SetTrigger("Sparkle");
        _spriteRenderer.enabled = false;
    }

    private void _onPickupAnimComplete()
    {
        Runner.Despawn(GetComponent<NetworkObject>());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.gameObject.GetComponent<Player_HostMode>();

        if (player != null)
        {
            _onPowerupPickedUp();
            player.ApplyPowerup(_powerupType);
        }
    }
}

public enum PowerupType
{
    None,
    Strength,
    Speed,
    Time
}
