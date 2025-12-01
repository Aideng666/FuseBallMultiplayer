using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkManager : SimulationBehaviour, IPlayerJoined
{
    public GameObject playerPrefab;
    private List<Player> _playerList;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        }
    }
}
