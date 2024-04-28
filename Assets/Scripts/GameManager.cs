using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    [field:SerializeField] public int MaxConnections { get; private set; }
    public static GameManager Instance { get; private set; }
    public RaceController CurrentRace { get; private set; }

    [HideInInspector] public NetworkVariable<int> NumPlayers;
    public NetworkVariable<PlayerInfo> HostInfo;
    public NetworkList<PlayerInfo> PlayerInfos;
    
    [SerializeField] private Player _playerPrefab;

    private NetworkManager _networkManager;
    private MainMenuUI _mainMenuUI;


    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        

        NumPlayers = new NetworkVariable<int>();
        NumPlayers.Value = 0;
        PlayerInfos = new();
        HostInfo = new();
    }

    private void Start()
    {
        _networkManager = NetworkManager.Singleton;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _mainMenuUI = FindObjectOfType<MainMenuUI>();
    }

    private void OnDisable()
    {
        _networkManager.OnClientConnectedCallback -= OnClientConnected;
    }


    public async void StartHost(Action<string> sucessCallback, Action failCallback)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        try //intenta crear un host
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            _networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            _networkManager.StartHost();

            sucessCallback(joinCode); //llama al callback
        }
        catch(Exception e) // si falla llama al callback de error
        {
            Debug.LogError(e);
            failCallback();
        }
    }

    public async void StartClient(string joinCode, Action successCallback, Action failCallback)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        try //intenta unirse a la sala con el joinCode
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            _networkManager.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            _networkManager.StartClient();
            successCallback(); //callback cuando termina
        }
        catch (Exception e) //callback de fallo si falla
        {
            Debug.LogError(e);
            failCallback();
        }
    }

    
    public override void OnNetworkSpawn() 
    {
        //se obtienen los datos del jugador que se haya unido / haya creado la sala
        var id = _networkManager.LocalClientId;
        var playerName = new FixedString64Bytes(_mainMenuUI.PlayerName);
        var playerColor = _mainMenuUI.PlayerColor;
        
        if (IsHost)  //si es el host guardamos su info duplicada en la variable HostInfo
        {
            HostInfo.Value = new PlayerInfo(id, playerName, playerColor);
        }
        
        AddPlayerInfoServerRpc(id, playerName, playerColor); //Llamada al server para actualizar la lista de PlayerInfos
        
        base.OnNetworkSpawn();
    }
    
    
    private void OnClientConnected(ulong id) { }
    

    
    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerInfoServerRpc(ulong id, FixedString64Bytes playerName, PlayerInfo.PlayerColor playerColor)
    {
        PlayerInfos.Add(new PlayerInfo(id, playerName, playerColor));
        NumPlayers.Value++;
    }
}