using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [HideInInspector] public int Index;

    [SerializeField] private GameObject _visualPrefab;
    
    private readonly HashSet<Player> _checkedPlayers = new();
    private RaceController _raceController;
    private CircuitController _circuit;
    private GameObject _visual;

    private void Start()
    {
        _raceController = GameManager.Instance.RaceController;
        _circuit = _raceController.CircuitController;
        var t = transform;
        _visual = Instantiate(_visualPrefab, t.position, t.rotation, t);
        _visual.transform.localScale = GetComponent<BoxCollider>().size;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsHost || !other.TryGetComponent<ICarController>(out var carController)) return;
        
        var player = (carController as Component).GetComponentInParent<Player>();

        for (int i = 0; i < _circuit.Checkpoints.Length; i++)
        {
            if (i < Index && !_circuit.Checkpoints[i].IsPlayerChecked(player)) return;
        }

        _checkedPlayers.Add(player);
        _raceController.UpdateCheckpointVisual(player.ID, Index, false);
    }

    public bool IsPlayerChecked(Player p) => _checkedPlayers.Contains(p);

    public void UncheckPlayer(Player p)
    {
        _checkedPlayers.Remove(p);
        _raceController.UpdateCheckpointVisual(p.ID, Index, true);
    }

    public void CheckPlayer(Player p) => _checkedPlayers.Add(p);

    public void ToggleVisual(bool active) => _visual.SetActive(active);

}
