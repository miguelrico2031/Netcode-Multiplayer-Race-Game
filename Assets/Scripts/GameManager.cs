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
    public NetworkVariable<Circuit> SelectedCircuit;
    
    [SerializeField] private Player _playerPrefab;

    private NetworkManager _networkManager;
    private MainMenuUI _mainMenuUI;

    private bool _disconnectedLocally = false;

    

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        

        NumPlayers = new NetworkVariable<int>();
        NumPlayers.Value = 0;
        PlayerInfos = new();
        HostInfo = new();
        SelectedCircuit = new();
    }

    private void Start()
    {
        _networkManager = NetworkManager.Singleton;
        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _networkManager.OnServerStopped += OnServerStopped;
        _networkManager.OnClientStopped += OnClientStopped;
        _mainMenuUI = FindObjectOfType<MainMenuUI>();
    }

    private void OnDisable()
    {

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
            SelectedCircuit.Value = _mainMenuUI.SelectedCircuit;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        AddPlayerInfoServerRpc(id, playerName, playerColor); //Llamada al server para actualizar la lista de PlayerInfos
        
        base.OnNetworkSpawn();
    }
    
    
    private void OnClientConnected(ulong id) { }

    private void OnClientDisconnected(ulong id)
    {
        Debug.Log(id + "Desconectado");
        NumPlayers.Value--;
        int i;
        for (i = 0; i < PlayerInfos.Count; i++)
            if (PlayerInfos[i].ID == id) break;
        PlayerInfos.RemoveAt(i);
        
    }

    private void OnServerStopped(bool b) //se llama local en el host cuando se para el servber
    {
        Debug.Log($"stopeado servero y el bool es {b}");
        ReturnToMainMenu();
    }
    
    private void OnClientStopped(bool b) //se llama en el cliente local solo (no en host) cuando se desconecta
    {
        Debug.Log($"stopeado cliento y el bool es {b}");
        if (_disconnectedLocally) ReturnToMainMenu();
        else _mainMenuUI.ShowErrorPanel(ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        _networkManager.OnClientConnectedCallback -= OnClientConnected;
        _networkManager.OnServerStopped -= OnServerStopped;
        _networkManager.OnClientStopped -= OnClientStopped;
        SceneManager.LoadScene(0);
    }

    public void Disconnect()
    {
        _disconnectedLocally = true;
        _networkManager.Shutdown();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerInfoServerRpc(ulong id, FixedString64Bytes playerName, PlayerInfo.PlayerColor playerColor)
    {
        PlayerInfos.Add(new PlayerInfo(id, playerName, playerColor));
        NumPlayers.Value++;
    }
}