using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class RaceController : NetworkBehaviour
{
    public event Action RaceStarted; //variable del host solo (de momento)
    public NetworkVariable<int> RaceCountdown;
    public CircuitController CircuitController { get; private set; }
    public NetworkVariable<FixedString64Bytes> PlayerOrder { get; private set; }

    [Serializable]
    public struct CircuitAndPrefab
    {
        public Circuit Circuit;
        public CircuitController Prefab;
    }

    [SerializeField] private float _checkRaceOrderDelta;
    [SerializeField] private CircuitAndPrefab[] _circuits;
    [SerializeField] private bool _showDebugSpheres; 
    
    private readonly List<Player> _players = new(); //solo host de momento
    private  List<Player> _sortedPlayers = new(); //solo host de momento
    private GameObject[] _debuggingSpheres; // a saber donde esto creo host

    private Coroutine StartRaceCor = null;

    private float[] _arcLengths;

    private float _raceOrderTimer = 0f;
    private bool _raceStarted;

    private float _playerUpdateArcTimer; //para actualizar solo 1vez/segundo los arcos en los jugadores (para comprobar si van para atras)
    
    #region Callbacks

    private void Awake()
    {
        RaceCountdown = new();
        GameManager.Instance.RaceController = this;
        PlayerOrder = new();
    }

    public override void OnNetworkSpawn()
    {
        SpawnCircuit();
        
        if(_showDebugSpheres) InitDebugSpheres();

        //GameManager.Instance.SpawnPlayer();

        if(IsHost)
        {
            Invoke(nameof(SpawnPlayerClientRpc), 2f);
        }
        
        base.OnNetworkSpawn();
    }



    [ClientRpc(RequireOwnership = false)]
    private void SpawnPlayerClientRpc()
    {
        GameManager.Instance.SpawnPlayer();
    }


    private void Update()
    {
        if (!IsSpawned || !IsHost) return;

        _raceOrderTimer += Time.deltaTime;
        _playerUpdateArcTimer += Time.deltaTime;
        if(_raceOrderTimer >= _checkRaceOrderDelta)
        {
            _raceOrderTimer = 0f;
            UpdateRaceProgress();
        }
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
        _sortedPlayers.Add(player);
        player.IndexInRaceController = _players.Count - 1;
        // player.car.transform.position = CircuitController.GetStartPos(player.StartOrder);
        player.car.GetComponent<ICarController>().ServerSetCarTransform(CircuitController.GetStartPos(player.StartOrder), player.car.transform.rotation);
        
        if (_players.Count == GameManager.Instance.NumPlayers.Value && StartRaceCor is null)
            StartRaceCor = StartCoroutine(StartRaceCountdown());

        if (GameManager.Instance.TrainingMode && StartRaceCor is null)
        {
            StartRaceCor = StartCoroutine(StartRaceCountdown());
        }
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
        _raceStarted = true;
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

        bool updateArc = _raceStarted && _playerUpdateArcTimer >= 1f;
        for (int i = 0; i < _players.Count; ++i)
        {
            _arcLengths[i] = ComputeCarArcLength(i);
            if (updateArc) _players[i].ArcLength.Value = _arcLengths[i];
        }
        if(updateArc) _playerUpdateArcTimer = 0f;

        _sortedPlayers.Sort(new PlayerOrderComparer(_arcLengths));

        UpdatePlayerOrderInfo();

    }

    private float ComputeCarArcLength(int idx)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = _players[idx].car.transform.position;


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

    private void UpdatePlayerOrderInfo()
    {
        var s = "";
        foreach (var p in _sortedPlayers)
        {
            s += $"{p.Name};";
        }

        PlayerOrder.Value = new FixedString64Bytes(s);

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