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
    public static GameManager Instance { get; private set; }
    public RaceController CurrentRace { get; private set; }
    // public int MaxConnections { get => _maxConnections; }


    public NetworkList<FixedString64Bytes> PlayerNames;
    public NetworkList<ulong> PlayerIDs;
    [HideInInspector] public NetworkVariable<int> NumPlayers;
    [HideInInspector] public NetworkVariable<FixedString64Bytes> HostName;
    [HideInInspector] public NetworkVariable<ulong> HostID;
    
    [SerializeField] private Player _playerPrefab;
    [SerializeField] private int _maxConnections;

    private NetworkManager _networkManager;
    private MainMenuUI _mainMenuUI;


    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //_playerInfos = new();
        NumPlayers = new NetworkVariable<int>();
        NumPlayers.Value = 0;
        HostName = new NetworkVariable<FixedString64Bytes>();
        PlayerNames = new NetworkList<FixedString64Bytes>();
        PlayerIDs = new NetworkList<ulong>();
        HostID = new();
    }

    private void Start()
    {
        _networkManager = NetworkManager.Singleton;
        if(_networkManager != null) _networkManager.OnClientConnectedCallback += OnClientConnected;
        _mainMenuUI = FindObjectOfType<MainMenuUI>();
    }

    public override void OnDestroy()
    {
        _networkManager.OnClientConnectedCallback -= OnClientConnected;
        
        base.OnDestroy();
    }




    public async void StartHost(Action<string> callback)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(_maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "dtls"));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        NetworkManager.Singleton.StartHost();

        callback(joinCode);
    }

    public async void StartClient(string joinCode, Action successCallback, Action failCallback)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            successCallback();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            failCallback();
        }
    }

    public override void OnNetworkSpawn()
    {
        var playerName = new FixedString64Bytes(_mainMenuUI.PlayerName); 
        var id = _networkManager.LocalClientId;
        
        if (IsHost)
        {
            HostName.Value = playerName;
            HostID.Value = id;
        }
        
        AddPlayerServerRpc(playerName, id);
        _mainMenuUI.OnPlayersUpdated();
        PlayerNames.OnListChanged += _mainMenuUI.OnPlayersUpdated;
        

    }
    


    private void OnClientConnected(ulong id)
    {

    }

    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerServerRpc(FixedString64Bytes playerName, ulong id)
    {
        PlayerNames.Add(playerName);
        PlayerIDs.Add(id);
        NumPlayers.Value++;
    }
}