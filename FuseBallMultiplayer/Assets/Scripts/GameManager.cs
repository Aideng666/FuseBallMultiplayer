using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private List<Transform> playerSpawnPoints;
    [SerializeField] private List<Ball> balls;
    [SerializeField] private List<Transform> ballStartPoints;
    
    private List<Player> _playerList;
    private bool _gameStarted;

    public override void Spawned()
    {
        if (!HasStateAuthority)
        {
            return;
        }
        
        _playerList = new List<Player>();
        _gameStarted = false;
        
        Player.OnPlayerJoined += _onPlayerConnected;
    }

    private void _startGame()
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

        foreach (var player in _playerList)
        {
            player.SetGameStartedRPC(true);
        }
        
        print("Starting Game");
    }

    private void _onPlayerConnected(Player player)
    {
        if (!HasStateAuthority)
        {
            return;
        }
        
        _playerList.Add(player);

        player.SetStartPositionRPC(playerSpawnPoints[_playerList.Count - 1].position);

        if (_playerList.Count == 2)
        {
            _startGame();
        }
    }
}
