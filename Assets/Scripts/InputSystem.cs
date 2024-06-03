using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Events;

public class InputSystem : MonoBehaviour
{
    public static InputSystem Instance { get; private set; }
    private Player _player;
    
    
    private InputAction _move, _brake, _attack, _reset;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if(_player) SetPlayer(_player);
        }
        else Destroy(gameObject);
    }



    public void SetPlayer(Player player)
    {
        _player = player;
        var input = _player.GetComponent<IInputController>();
        var playerInput = FindObjectOfType<PlayerInput>();
        
        _move = playerInput.actions["Move"];
        _move.performed += input.OnMove;
        _move.canceled += input.OnMove;
        _move.Enable();
        
        _brake = playerInput.actions["Brake"];
        _brake.performed += input.OnBrake;
        _brake.canceled += input.OnBrake;
        _brake.Enable();
        
        _attack = playerInput.actions["Attack"];
        _attack.performed += input.OnAttack;
        _attack.Enable();

        _reset = playerInput.actions["Reset"];
        _reset.performed += input.OnReset;
        _reset.Enable();
    }
}