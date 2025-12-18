using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager_HostMode : NetworkBehaviour
{
    [SerializeField] private HUDController hud;
    [SerializeField] private NetworkPrefabRef powerupPrefab;
    [SerializeField] private List<Ball_HostMode> balls;
    [SerializeField] private int powerupSpawnDelay;
    [Networked] public bool AllPlayersReady { get; set; }
    [Networked] public int DeadPlayerNum { get; set; }
    [Networked] public bool GameStarted { get; set; }
    [Networked] public Player_HostMode Player1 { get; set; }
    [Networked] public Player_HostMode Player2 { get; set; }
    
    private ChangeDetector _changeDetector;
    private Player_HostMode[] _players = new Player_HostMode[2];

    private bool _powerOnField;
    private float _elapsedPowerupSpawnDelay;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        hud.OnGameStarted += _startGame;
        
        Player1 = null;
        Player2 = null;
        DeadPlayerNum = 0;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        hud.OnGameStarted -= _startGame;
    }

    public override void FixedUpdateNetwork()
    {
        if (!_powerOnField && Runner.IsServer && GameStarted)
        {
            if (_canSpawnPowerup())
            {
                _spawnPowerup();
            }

            _elapsedPowerupSpawnDelay += Time.deltaTime;
        }
    }

    public override void Render()
    {
        if (GameStarted && Player1 != null && Player2 != null)
        {
            hud.UpdateFuses(Player1, Player2);
        }
        
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(AllPlayersReady):

                    if (AllPlayersReady)
                    {
                        hud.PlayGameStartSequence();
                    }

                    break;
                
                case nameof(DeadPlayerNum):

                    if (DeadPlayerNum == 1)
                    {
                        hud.ShowGameOver(2);
                    }
                    else if (DeadPlayerNum == 2)
                    {
                        hud.ShowGameOver(1);
                    }
                    else if (DeadPlayerNum == 0)
                    {
                        return;
                    }
                    
                    if (Runner.IsServer)
                    {
                        foreach (var player in _players)
                        {
                            player.SetGameStarted(false);
                        }
                    }
                    
                    break;
                
                case nameof(Player1):
                    
                    _players[0] = Player1;

                    if (Player1 != null)
                    {
                        Player1.OnPlayerReadyChanged += _onPlayerReadyChanged;
                        Player1.OnPlayerDied += _onPlayerDied;
                    }

                    break;
                
                case nameof(Player2):
                    
                    _players[1] = Player2;

                    if (Player2 != null)
                    {
                        Player2.OnPlayerReadyChanged += _onPlayerReadyChanged;
                        Player2.OnPlayerDied += _onPlayerDied;
                    }

                    break;
            }
        }
    }
    
    private void _startGame()
    {
        if (Runner.IsServer)
        {
            foreach (var player in _players)
            {
                player.SetGameStarted(true);
            }

            GameStarted = true;
            DeadPlayerNum = 0;
        }
    }

    private void _onPlayerDied(Player_HostMode deadPlayer)
    {
        if (!GameStarted || !Runner.IsServer)
        {
            return;
        }

        AllPlayersReady = false;
        DeadPlayerNum = deadPlayer.PlayerNum;
        GameStarted = false;
    }

    private void _onPlayerReadyChanged(Player_HostMode player)
    {
        int readyCount = _players.Count(p => p.IsReady);
        
        hud.UpdateReadyText(readyCount);

        if (readyCount == _players.Length && Runner.IsServer)
        {
            foreach (var p in _players)
            {
                p.ResetPlayer = true;
            }

            foreach (var ball in balls)
            {
                ball.ResetBallPosition = true;
            }

            AllPlayersReady = true;
        }
    }
    
    private void _spawnPowerup()
    {
        bool positionValid = false;
        Vector2 powerupPosition = Vector2.zero;

        while (!positionValid)
        {
            powerupPosition = new Vector2(Random.Range(-14, 14), Random.Range(-5, 5));
            
            var closeColliders = Physics2D.OverlapCircleAll(powerupPosition, 4);
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
                positionValid = true;
            }
        }

        var spawnedPowerup = Runner.Spawn(powerupPrefab, powerupPosition, Quaternion.identity);
        var powerup = spawnedPowerup.GetComponent<Powerup>();
        powerup.OnPowerupPickedUp += _onPowerupPickedUp;

        _powerOnField = true;
        _elapsedPowerupSpawnDelay = 0;
    }
    
    private bool _canSpawnPowerup()
    {
        if (_elapsedPowerupSpawnDelay >= powerupSpawnDelay)
        {
            return true;
        }

        return false;
    }
    
    private void _onPowerupPickedUp(Powerup powerup)
    {
        _powerOnField = false;
        powerup.OnPowerupPickedUp -= _onPowerupPickedUp;
    }
}
