using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class HostModeSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef player1Prefab;
    [SerializeField] private NetworkPrefabRef player2Prefab;
    [SerializeField] private LobbyUI lobbyUI;
    [SerializeField] private HUDController hud;
    [SerializeField] private GameManager_HostMode gameManager;
    
    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private List<Player_HostMode> _players = new List<Player_HostMode>();
    
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction strikeAction;
    private InputAction dodgeAction;

    private int _numPlayersReady;
    private bool _gameStarted;
    private bool _powerOnField;
    private float _elapsedPowerupSpawnDelay;
    
    private void Awake()
    {
        lobbyUI.OnPlayerJoinedSession += _onPlayerConnected;
        
        Player_HostMode.OnPlayerSpawned += _onPlayerSpawned;
        Player_HostMode.OnPlayerDespawned += _onPlayerDespawned;
    }

    private void OnDestroy()
    {
        lobbyUI.OnPlayerJoinedSession -= _onPlayerConnected;
        
        Player_HostMode.OnPlayerSpawned -= _onPlayerSpawned;
        Player_HostMode.OnPlayerDespawned -= _onPlayerDespawned;
    }

    async void StartLobby(GameMode mode, string sessionName)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        
        var runnerSimulatePhysics2D = gameObject.AddComponent<RunnerSimulatePhysics2D>();
        runnerSimulatePhysics2D.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;
        
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        strikeAction = playerInput.actions["Strike"];
        dodgeAction = playerInput.actions["Dodge"];
        
        playerInput.actions.Enable();
    }

    private void _onPlayerConnected(GameMode mode, string sessionID)
    {
        if (_runner == null)
        {
            StartLobby(mode, sessionID);
            lobbyUI.gameObject.SetActive(false);
        }
    }

    private void _onPlayerSpawned(Player_HostMode spawnedPlayer)
    {
        _players.Add(spawnedPlayer);

        if (_runner.IsServer && _players.Count == 1)
        {
            spawnedPlayer.PlayerNum = 1;
            gameManager.Player1 = spawnedPlayer;
        }
        else if (_players.Count == 2)
        {
            hud.gameObject.SetActive(true);

            if (_runner.IsServer)
            {
                spawnedPlayer.PlayerNum = 2;
                gameManager.Player2 = spawnedPlayer;

                gameManager.Player1.GameSetupComplete = true;
                gameManager.Player2.GameSetupComplete = true;
            }
        }
    }
    
    private void _onPlayerDespawned(Player_HostMode player)
    {
        _players.Remove(player);

        if (_runner.IsServer)
        {
            if (player.PlayerNum == 1)
            {
                gameManager.Player1 = null;
            }
            else if (player.PlayerNum == 2)
            {
                gameManager.Player2 = null;
            }
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Vector3 spawnPosition = Vector3.zero;
            NetworkPrefabRef prefabToSpawn = new NetworkPrefabRef();
            if (_spawnedPlayers.Count == 0)
            {
                spawnPosition = new Vector3(-5, 0, 0);
                prefabToSpawn = player1Prefab;
            }
            else if (_spawnedPlayers.Count == 1)
            {
                spawnPosition = new Vector3(5, 0, 0);
                prefabToSpawn = player2Prefab;
            }
            
            NetworkObject networkPlayerObject = runner.Spawn(prefabToSpawn, spawnPosition, Quaternion.identity, player);
            _spawnedPlayers.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedPlayers.Remove(player);
        }
    }
    
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData inputData = new NetworkInputData();
        
        inputData.Buttons.Set(NetworkInputButtons.Strike, strikeAction.IsPressed());
        inputData.Buttons.Set(NetworkInputButtons.Dodge, dodgeAction.IsPressed());

        inputData.MoveDirection = moveAction.ReadValue<Vector2>();

        input.Set(inputData);
    }
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }
}
