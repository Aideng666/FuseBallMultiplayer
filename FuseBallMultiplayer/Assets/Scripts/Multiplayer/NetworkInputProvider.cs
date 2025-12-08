using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction strikeAction;
    private InputAction dodgeAction;
    
    private bool _isInitialized = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        strikeAction = playerInput.actions["Strike"];
        dodgeAction = playerInput.actions["Dodge"];
        
        playerInput.actions.Enable();
    }

    private void OnEnable()
    {
        StartCoroutine(_registerCallbacks());
    }

    private void OnDisable()
    {
        var runner = GetComponent<NetworkRunner>();
        runner.RemoveCallbacks(this);
    }

    private IEnumerator _registerCallbacks()
    {
        NetworkRunner runner = null;
        while (runner == null)
        {
            runner = GetComponent<NetworkRunner>();

            if (!runner.ProvideInput)
            {
                print("Runner was not providing input, now it is");
                runner.ProvideInput = true;
            }
            
            yield return null; 
        }
        
        runner.AddCallbacks(this);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData inputData = new NetworkInputData();
        
        inputData.Buttons.Set(NetworkInputButtons.Strike, strikeAction.IsPressed());
        inputData.Buttons.Set(NetworkInputButtons.Dodge, dodgeAction.IsPressed());

        inputData.MoveDirection = moveAction.ReadValue<Vector2>();

        input.Set(inputData);
    }

    #region UnusedNetworkFunctions
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        
    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        
    }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }
    #endregion
}
