using UnityEngine;
using System;
using Unity.Netcode;
using UnityEngine.InputSystem.HID;

public interface ICarController
{
    public float InputAcceleration { get; set; }
    public float InputSteering { get; set; }
    public float InputBrake { get; set; }
    public Transform GoalCheck { get; }

    public void RepositionCar(Action onRepositionedCallback);

    public void ServerSetCarTransform(Vector3 pos, Quaternion rot);

    public void ShowOverturnText();
    public void HideOverturnText();

}