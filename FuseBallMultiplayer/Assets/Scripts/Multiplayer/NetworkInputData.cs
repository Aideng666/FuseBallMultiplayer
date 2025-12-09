using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkButtons Buttons;
}

enum NetworkInputButtons
{
    Strike = 0,
    Dodge = 1
}
