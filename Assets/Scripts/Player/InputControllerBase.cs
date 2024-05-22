using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InputControllerBase : NetworkBehaviour, IInputController
{
    public bool InputEnabled { get; set; }
    
    
    private Player _player;
    private ICarController _car;

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
}