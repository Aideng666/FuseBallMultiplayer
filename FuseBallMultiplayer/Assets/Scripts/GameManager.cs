using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private HUDController hud;
    [SerializeField] private List<Transform> playerSpawnPoints;
    [SerializeField] private List<Ball> balls;
    [SerializeField] private List<Transform> ballStartPoints;
    
    private List<Player> _playerList;
    private int _numPlayersReady;
    private bool _isReady;
    private bool _gameStarted;
    
    [Networked, OnChangedRender(nameof(_onGameSetupChanged))]
    public bool GameSetupComplete { get; set; }
    
    [Networked, OnChangedRender(nameof(_onGameStartedChanged))]
    public bool GameStarted { get; set; }

    [Networked]
    public Player Player1 { get; set; }
    
    [Networked]
    public Player Player2 { get; set; }

    [Networked, OnChangedRender(nameof(_onNumPlayersReadyChanged))]
    public int NumPlayersReady { get; set; }

    public override void Spawned()
    {
        _playerList = new List<Player>();
        
        if (HasStateAuthority)
        {
            Player.OnPlayerJoined += _onPlayerConnected;
            hud.OnGameStarted += _startGame;
        }
        
        hud.gameObject.SetActive(false);
        _numPlayersReady = 0;
        _isReady = false;
        _gameStarted = false;
        GameStarted = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasStateAuthority)
        {
            Player.OnPlayerJoined -= _onPlayerConnected;
            hud.OnGameStarted -= _startGame;
        }

        if (Player1 != null)
        {
            Player1.OnPlayerReady -= _playerReadyRPC;
        }
        if (Player2 != null)
        {
            Player2.OnPlayerReady -= _playerReadyRPC;
        }
    }

    private void Update()
    {
        /*if (_gameStarted)
        {
            hud.UpdateFuses(Player1, Player2);
        }*/
    }

    private void _startGame()
    {
        if (!HasStateAuthority)
        {
            return;
        }
        
        foreach (var player in _playerList)
        {
            player.SetGameStartedRPC(true);
        }
        
        GameStarted = true;
    }

    private void _setupGame()
    {
        if (!HasStateAuthority)
        {
            return;
        }
        
        for (int i = 0; i < balls.Count; i++)
        {
            balls[i].transform.position = ballStartPoints[i].position;
            balls[i].SetActive(true);
        }

        GameSetupComplete = true;
        
        foreach (var player in _playerList)
        {
            player.SetGameSetupCompleteRPC(true);
        }
    }

    private void _onPlayerConnected(Player player)
    {
        if (!HasStateAuthority)
        {
            return;
        }

        _playerList.Add(player);
        if (_playerList.Count == 1)
        {
            Player1 = player;
        }
        else if (_playerList.Count == 2)
        {
            Player2 = player;
        }

        player.SetStartPositionRPC(playerSpawnPoints[_playerList.Count - 1].position);

        if (_playerList.Count == 2)
        {
            _setupGame();
        }

        player.OnPlayerReady += _playerReadyRPC;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void _beginGameStartSequenceRPC()
    {
        hud.PlayGameStartSequence();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void _playerReadyRPC(bool isReady)
    {
        if (isReady)
        {
            NumPlayersReady++;
        }
        else
        {
            NumPlayersReady--;
        }

        if (NumPlayersReady == _playerList.Count)
        {
            _beginGameStartSequenceRPC();
        }
    }

    private void _onGameSetupChanged()
    {
        hud.gameObject.SetActive(true);
    }
    
    private void _onGameStartedChanged()
    {
        _gameStarted = GameStarted;
    }

    private void _onNumPlayersReadyChanged()
    {
        _numPlayersReady = NumPlayersReady;
        hud.UpdateReadyText(_numPlayersReady);
    }
}
