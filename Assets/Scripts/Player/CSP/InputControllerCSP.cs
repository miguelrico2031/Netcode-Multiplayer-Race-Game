using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InputControllerCSP : NetworkBehaviour, IInputController
{
    public bool InputEnabled { get => _inputEnabled.Value; set => _inputEnabled.Value = value; }
    public NetworkVariable<bool> _inputEnabled;
    
    private Player _player;
    private ICarController _car;


    private void Awake()
    {
        _inputEnabled = new() {Value = false};
    }

    private void Start()
    {
        _player = GetComponent<Player>();
        _car = _player.car.GetComponent<ICarController>();
        
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            GameManager.Instance.RaceController.RaceStarted += OnRaceStarted;
        }
        base.OnNetworkSpawn();
    }

    private void OnRaceStarted()
    {
        GameManager.Instance.RaceController.RaceStarted -= OnRaceStarted;
        InputEnabled = true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>();
        if (!InputEnabled) input = Vector2.zero;
        _car.InputAcceleration = input.y;
        _car.InputSteering = input.x;

    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<float>();
        if (!InputEnabled) input = 0f;
        _car.InputBrake = input;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }
    
}