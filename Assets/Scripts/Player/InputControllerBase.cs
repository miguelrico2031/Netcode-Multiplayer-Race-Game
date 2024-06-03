using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InputControllerBase : NetworkBehaviour, IInputController
{
    public bool InputEnabled
    {
        get => _serverInputEnabled.Value && _clientInputEnabled;
        set
        {
            if (!IsSpawned) return;
            if (IsHost) _serverInputEnabled.Value = value;
            else if (IsOwner) _clientInputEnabled = value;
        }
    }
    public NetworkVariable<bool> _serverInputEnabled;

    private bool _clientInputEnabled = true;


    private Player _player;
    private ICarController _car;

    public void Awake()
    {
        _serverInputEnabled = new() { Value = false};
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
        OnMoveServerRpc(context.ReadValue<Vector2>());

    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        OnBrakeServerRpc(context.ReadValue<float>());
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.started) OnAttackServerRpc();
    }

    public void OnReset(InputAction.CallbackContext context)
    {
        OnResetServerRpc();
    }
    
    
    //no se si hace falta que sea public
    [ServerRpc]
    private void OnMoveServerRpc(Vector2 input)
    {
        if (!InputEnabled) input = Vector2.zero;
        _car.InputAcceleration = input.y;
        _car.InputSteering = input.x;
    }
    
    [ServerRpc]
    private void OnBrakeServerRpc(float input)
    {
        if (!InputEnabled) input = 0f;
        _car.InputBrake = input;
    }
    
    [ServerRpc]
    private void OnAttackServerRpc()
    {
        if (!InputEnabled) return;
    }

    [ServerRpc]
    private void OnResetServerRpc()
    {
        if (InputEnabled)
            _car.RepositionCar(() => { });
    }
}