using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : NetworkBehaviour
{
    private CarController car;

    private void Start()
    {
        car = GetComponent<Player>().car.GetComponent<CarController>();

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
        car.InputAcceleration = input.y;
        car.InputSteering = input.x;
    }
    
    [ServerRpc]
    private void OnBrakeServerRpc(float input)
    {
        car.InputBrake = input;
    }
    
    [ServerRpc]
    private void OnAttackServerRpc()
    {
        
    }
}