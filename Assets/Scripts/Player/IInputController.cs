using UnityEngine.InputSystem;

public interface IInputController
{
    public bool InputEnabled { get; set; }

    public void OnMove(InputAction.CallbackContext context);
    public void OnBrake(InputAction.CallbackContext context);
    public void OnAttack(InputAction.CallbackContext context);
}