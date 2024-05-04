using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GoalController : MonoBehaviour
{
    public event Action<Player> OnPlayerFinish;


    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsHost || !other.TryGetComponent<CarController>(out var carController)) return;

        var player = carController.GetComponentInParent<Player>();
        var circuitController = GameManager.Instance.RaceController.CircuitController;

        bool allChecked = circuitController.Checkpoints.All(checkpoint => checkpoint.IsPlayerChecked(player));
        
        circuitController.ComputeClosestPointArcLength(carController.GoalCheck.position, out int segmentIdx, out _, out _);
        bool rightdirection = segmentIdx != 0;

            if (allChecked && rightdirection)
        {
            player.CurrentLap.Value++;
            if (player.CurrentLap.Value > 3)
            {
                OnPlayerFinish?.Invoke(player);
            }
        }
        
        foreach(var checkpoint in circuitController.Checkpoints) checkpoint.UncheckPlayer(player);
        
    }
}
