using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class RaceController : NetworkBehaviour
{
    public event Action RaceStarted; //variable del host solo (de momento)
    public NetworkVariable<int> RaceCountdown;
    public CircuitController CircuitController { get; private set; }
    
    [Serializable]
    public struct CircuitAndPrefab
    {
        public Circuit Circuit;
        public CircuitController Prefab;
    }
    
    [SerializeField] private CircuitAndPrefab[] _circuits;
    [SerializeField] private bool _showDebugSpheres; 
    
    private readonly List<Player> _players = new(); //solo host de momento
    private GameObject[] _debuggingSpheres; // a saber donde esto creo host

    private Coroutine StartRaceCor = null;

    private float[] _arcLengths;
    
    #region Callbacks

    private void Awake()
    {
        RaceCountdown = new();
        GameManager.Instance.RaceController = this;
    }

    public override void OnNetworkSpawn()
    {
        SpawnCircuit();
        
        if(_showDebugSpheres) InitDebugSpheres();

        GameManager.Instance.SpawnPlayer();
        
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsSpawned || !IsHost) return;

        UpdateRaceProgress();
    }
    
    #endregion
    
    
    #region Initialization
    
    private void SpawnCircuit()
    {
        var circuit = GameManager.Instance.SelectedCircuit.Value;
        var prefab = _circuits[0].Prefab;
        foreach (var c in _circuits)
        {
            if (c.Circuit != circuit) continue;
            prefab = c.Prefab;
        }
        CircuitController = Instantiate(prefab, prefab.transform.position, Quaternion.identity, transform);

    }

    private void InitDebugSpheres()
    {
        int nPlayers = GameManager.Instance.NumPlayers.Value;
        _debuggingSpheres = new GameObject[nPlayers];
        for (int i = 0; i < nPlayers; ++i)
        {
            _debuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _debuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
        }

        CircuitController.GetComponent<LineRenderer>().enabled = true;
    }
    
    
    #endregion

    
    
    
    public void AddPlayer(Player player) //solo se llama en el Host
    {
        _players.Add(player);
        player.IndexInRaceController = _players.Count - 1;
        player.car.transform.position = CircuitController.GetStartPos(player.StartOrder);

        if (_players.Count == GameManager.Instance.NumPlayers.Value && StartRaceCor is null)
            StartRaceCor = StartCoroutine(StartRaceCountdown());
    }

    private IEnumerator StartRaceCountdown()
    {
        yield return new WaitForSeconds(1f);
        RaceCountdown.Value = 3;
        yield return new WaitForSeconds(1f);
        RaceCountdown.Value = 2;
        yield return new WaitForSeconds(1f);
        RaceCountdown.Value = 1;
        yield return new WaitForSeconds(1f);
        RaceCountdown.Value = 0;
        RaceStarted?.Invoke();
    }

    private class PlayerOrderComparer : Comparer<Player>
    {
        private readonly float[] _arcLengths;

        public PlayerOrderComparer(float[] arcLengths)
        {
            _arcLengths = arcLengths;
        }

        public override int Compare(Player x, Player y)
        {
            if (_arcLengths[x.IndexInRaceController] < _arcLengths[y.IndexInRaceController])
                return 1;
            else return -1;
        }
    }

    private void UpdateRaceProgress() //solo se llama en el host
    {
        // Update car arc-lengths
        _arcLengths = new float[_players.Count];

        for (int i = 0; i < _players.Count; ++i)
        {
            _arcLengths[i] = ComputeCarArcLength(i);
        }

        _players.Sort(new PlayerOrderComparer(_arcLengths));

        // string myRaceOrder = "";
        // foreach (var player in _players)
        // {
        //     myRaceOrder += player.Name + " ";
        // }
        //
        // Debug.Log("Race order: " + myRaceOrder);
    }

    private float ComputeCarArcLength(int idx)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this._players[idx].car.transform.position;


        float minArcL =
            this.CircuitController.ComputeClosestPointArcLength(carPos, out _, out var carProj, out _);

        if(_showDebugSpheres) this._debuggingSpheres[idx].transform.position = carProj;

        
        // Esto no lo entiendo, si está en la 0 creo que debería dejarlo como esta
        
        // if (this._players[id].CurrentLap == 0)
        // {
        //     minArcL -= _circuitController.CircuitLength;
        // }
        // else
        // {
        //     minArcL += _circuitController.CircuitLength *
        //                (_players[id].CurrentLap - 1);
        // }
        
        minArcL += CircuitController.TotalLength * _players[idx].CurrentLap.Value;
        
        return minArcL;
    }

    public void UpdateCheckpointVisual(ulong id, int index, bool active)
    {
        UCVClientRpc(id, index, active);
    }

    [ClientRpc(RequireOwnership = false)]
    private void UCVClientRpc(ulong id, int index, bool active)
    {
        if (NetworkManager.Singleton.LocalClientId != id) return;
        CircuitController.Checkpoints[index].ToggleVisual(active);
    }
}