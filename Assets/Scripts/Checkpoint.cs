using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int Index;

    private readonly HashSet<Player> _checkedPlayers = new();
    private CircuitController _circuit;

    private void Start()
    {
        _circuit = GameManager.Instance.RaceController.CircuitController;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsHost || !other.TryGetComponent<CarController>(out var carController)) return;
        
        var player = carController.GetComponentInParent<Player>();

        for (int i = 0; i < _circuit.Checkpoints.Length; i++)
        {
            if (i < Index && !_circuit.Checkpoints[i].IsPlayerChecked(player)) return;
        }

        _checkedPlayers.Add(player);
    }

    public bool IsPlayerChecked(Player p) => _checkedPlayers.Contains(p);

    public void UncheckPlayer(Player p) => _checkedPlayers.Remove(p);



}
