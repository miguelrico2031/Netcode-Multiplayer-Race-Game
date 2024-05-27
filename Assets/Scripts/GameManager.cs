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
    public RaceController RaceController { get; set; } = null;
    public GameplayHUDUI HUD { get; set; } = null;
    public Player LocalPlayer;

    //variables de red
    [HideInInspector] public NetworkVariable<int> NumPlayers;
    [HideInInspector] public NetworkVariable<PlayerInfo> HostInfo;
    [HideInInspector] public NetworkVariable<Circuit> SelectedCircuit;
    public NetworkList<PlayerInfo> PlayerInfos;
    
    
    [SerializeField] private Player _playerPrefab;
    
    private NetworkManager _networkManager;
    private PlayerInfo _localPlayerInfo;
    private MainMenuUI _mainMenuUI;
    private readonly HashSet<ulong> _readyPlayers = new();
    private bool _disconnectedLocally = false;

    
    
    #region Unity Callbacks
    private void Awake()
    {
        //singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
        _networkManager.ConnectionApprovalCallback += ApprovalCheck;
        _mainMenuUI = FindObjectOfType<MainMenuUI>();
    }
    
    private void OnDisable()
    {

    }
    
    
    #endregion

    
    #region Main Menu Network Methods
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
        catch // si falla llama al callback de error
        {
            // Debug.LogError(e);
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
        catch  //callback de fallo si falla
        {
            // Debug.LogError(e);
            failCallback();
        }
    }
    
    private void LoadGameScene()
    { //carga la escena del juego
        _networkManager.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
    
    #endregion
    
    
    #region Main Menu Network Callbacks
    
    public override void OnNetworkSpawn() //se ejecuta en cada cliente o host 1 sola vez al spawnear el network object
    {
        //se obtienen los datos del jugador que se haya unido / haya creado la sala
        var id = _networkManager.LocalClientId;
        var playerName = new FixedString64Bytes(_mainMenuUI.PlayerName);
        var playerColor = _mainMenuUI.PlayerColor;
        _localPlayerInfo = new PlayerInfo(id, playerName, playerColor); //copia local para saber nuestro playerInfo
        if (IsHost)  //si es el host guardamos su info duplicada en la variable HostInfo
        {
            HostInfo.Value = new PlayerInfo(id, playerName, playerColor); //variable de red modificada solo por el Host
            SelectedCircuit.Value = _mainMenuUI.SelectedCircuit;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        AddPlayerInfoServerRpc(id, playerName, playerColor); //Llamada al server para actualizar la lista de PlayerInfos
        
        base.OnNetworkSpawn();
    }
    
    private void OnClientConnected(ulong id) { }
    
    #endregion


    #region General Network Callbacks
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    { //evita que clientes se puedan unir si se excede el maximo de usuarios o ya empezo una partida
        response.Approved = SceneManager.GetActiveScene().buildIndex == 0 && NumPlayers.Value < MaxConnections;
    }
    private void OnClientDisconnected(ulong id)
    {
        if (!IsHost) return; //Solo se ejecuta en el host
        //decrementa los jugadores y borra el playerInfo del cliente desconectado
        NumPlayers.Value--;
        int i;
        for (i = 0; i < PlayerInfos.Count; i++)
            if (PlayerInfos[i].ID == id) break;
        PlayerInfos.RemoveAt(i);
        _readyPlayers.Remove(id); //borra la peticion de iniciar partida del cliente desconectado (si la hubiera)
    }

    private void OnServerStopped(bool b) //se llama local en el host cuando se para el server
    {
        Debug.Log($"server parado -> {b}");
        ReturnToMainMenu();
    }
    
    private void OnClientStopped(bool b) //se llama en el cliente local solo cuando se desconecta
    {
        Debug.Log($"cliente parado -> {b}");
        if (_disconnectedLocally) ReturnToMainMenu(); //si se ha desconectado por voluntad propia
        else _mainMenuUI.ShowErrorPanel(ReturnToMainMenu); //si no, se muestra una ventana de error de conexion
    }
    
    #endregion
    
    
    #region General Network Methods
    public void Disconnect()
    {
        _disconnectedLocally = true;
        _networkManager.Shutdown();
    }
    
    private void ReturnToMainMenu()
    {
        _networkManager.OnClientConnectedCallback -= OnClientConnected;
        _networkManager.OnServerStopped -= OnServerStopped;
        _networkManager.OnClientStopped -= OnClientStopped;
        SceneManager.LoadScene(0); //carga el menu principal (asumiendo que el networkmanager esta apagado)
    }
    
    #endregion

    
    #region Main Menu RPCS
    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerInfoServerRpc(ulong id, FixedString64Bytes playerName, Player.PlayerColor playerColor)
    {
        NumPlayers.Value++;
        PlayerInfos.Add(new PlayerInfo(id, playerName, playerColor));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong id)
    { //si los jugadores listos para jugar son la mayoria (y hay al menos 2) empieza el juego
        _readyPlayers.Add(id);
        // if (NumPlayers.Value > 1 && _readyPlayers.Count > NumPlayers.Value / 2f) 
            LoadGameScene();
    }
    
    #endregion
    
    
    #region Game Methods


    public void SpawnPlayer() //se llama local y hace un rpc al server para spawnear el jugador
    {
        var id = _networkManager.LocalClientId;
        SpawnPlayerServerRpc(id);
    }
    
    #endregion
    
    #region Game RPCs
    
    private int tempOrder = 0;
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong id)
    {
       
        LocalPlayer = Instantiate(_playerPrefab);
        LocalPlayer.ID = id;
        LocalPlayer.StartOrder = tempOrder++;
        LocalPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(id);
    }
    #endregion
}