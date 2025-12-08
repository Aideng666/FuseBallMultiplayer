/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction strikeAction;
    private InputAction dodgeAction;
    private InputAction playAgainAction;

    public static InputManager Instance { get; set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        
        Instance = this;

        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        strikeAction = playerInput.actions["Strike"];
        dodgeAction = playerInput.actions["Dodge"];
        playAgainAction = playerInput.actions["PlayAgain"];
    }

    public Vector2 GetMoveInput()
    {
        return moveAction.ReadValue<Vector2>();
    }

    public bool StrikePressed()
    {
        return strikeAction.triggered;
    }

    public bool DodgePressed()
    {
        return dodgeAction.triggered;
    }

    public bool PlayAgainPressed()
    {
        return playAgainAction.triggered;
    }
}
*/
