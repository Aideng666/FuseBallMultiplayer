using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkButtons Buttons;
}

enum NetworkInputButtons
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
    Strike = 4,
    Dodge = 5
}
