using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InputControllerCSP : NetworkBehaviour, IInputController
{
    public bool InputEnabled 
    {
        get => _serverInputEnabled.Value && _clientInputEnabled;
        set
        {
            if (!IsSpawned) return;
            if(IsHost) _serverInputEnabled.Value = value;
            else if (IsOwner) _clientInputEnabled = value;
        }
    }
    public NetworkVariable<bool> _serverInputEnabled;

    private bool _clientInputEnabled = true;
    
    private Player _player;
    private ICarController _car;


    private void Awake()
    {
        _serverInputEnabled = new() {Value = false};
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