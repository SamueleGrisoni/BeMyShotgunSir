using BeMyShotgunSir.Scripts.Utils;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    #region ConnectionManager Enums
    public enum AppFlowState
    {
        Startup,
        Init,
        Lobby
    }

    public enum ConnectionFlowState
    {
        Idle,
        Hosting,
        Joining
    }
    #endregion

    public class ConnectionManager : MonoBehaviour
    {
        private bool _isInitialized = false;
        private bool _isHostSession = false;

        [SerializeField]
        private AppFlowState _currentAppFlow = AppFlowState.Startup;
        private ServerManager _serverManager;
        private ClientManager _clientManager;
        private int _pendingRemotePlayerDelta;
        [SerializeField] private ConnectionFlowState _connectionFlowState = ConnectionFlowState.Idle;
        [SerializeField] private NetworkObject _lobbyManagerPrefab;
        private NetworkObject _spawnedLobbyManagerInstance;

        private void Awake()
        {
            if (_lobbyManagerPrefab == null)
                Log.ELazy(() => "LobbyManager prefab reference is not assigned in the inspector.", this);

            HandleStartup();
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (GameServices.Instance == null || GameServices.Instance.NetworkManager == null)
            {
                Log.ELazy(() => "GameServices or NetworkManager is missing.", this);
                return;
            }

            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _clientManager = GameServices.Instance.NetworkManager.ClientManager;

            if (_serverManager == null || _clientManager == null)
            {
                Log.ELazy(() => "ServerManager or ClientManager is missing.", this);
                return;
            }

            _serverManager.OnServerConnectionState += OnServerConnectionState;
            _clientManager.OnClientConnectionState += OnClientConnectionState;

            Log.DLazy(() => "ConnectionManager initialized.", this);

            HandleInit();

            _isInitialized = true;
        }

        public void StartHost()
        {
            if (!_isInitialized)
                Initialize();

            _isHostSession = true;

            _connectionFlowState = ConnectionFlowState.Hosting;

            Log.DLazy(() => "Starting host session.", this);
            _serverManager.StartConnection();
            _clientManager.StartConnection();
        }

        public void StartJoin(string ipAddress)
        {
            if (!_isInitialized)
                Initialize();

            _isHostSession = false;
            _connectionFlowState = ConnectionFlowState.Joining;

            if (string.Equals(ipAddress, "localhost", System.StringComparison.OrdinalIgnoreCase))
                ipAddress = "127.0.0.1";
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetClientAddress(ipAddress);

            Log.DLazy(() => $"Starting join session to {ipAddress}.", this);
            _clientManager.StartConnection();
        }

        public void QuitLobby() =>
            HandleInit();

        private void HandleStartup()
        {
            if (_currentAppFlow == AppFlowState.Startup)
                return;

            _currentAppFlow = AppFlowState.Startup;
        }

        private void HandleInit()
        {
            if (_currentAppFlow == AppFlowState.Init)
                return;

            _currentAppFlow = AppFlowState.Init;

            if (_spawnedLobbyManagerInstance != null)
            {
                _spawnedLobbyManagerInstance.Despawn();
                _spawnedLobbyManagerInstance = null;
            }

            _serverManager.StopConnection(true);
            _clientManager.StopConnection();
            GameServices.Instance.SceneCoordinator.LoadInitScene();
        }

        private void HandleLobby()
        {
            if (_currentAppFlow == AppFlowState.Lobby)
                return;

            _currentAppFlow = AppFlowState.Lobby;

            if (!_isHostSession)
                return;
            //NOTE host only operations below this

            if (_lobbyManagerPrefab != null && _spawnedLobbyManagerInstance == null)
            {
                NetworkObject instance = Instantiate(_lobbyManagerPrefab);
                instance.gameObject.name = instance.gameObject.name.Replace("(Clone)", " Server");
                _serverManager.Spawn(instance);
                _spawnedLobbyManagerInstance = instance;
            }
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (_connectionFlowState == ConnectionFlowState.Hosting)
                    _connectionFlowState = ConnectionFlowState.Idle;

                Log.DLazy(() => "Server connection started.", this);
                HandleLobby();
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _connectionFlowState = ConnectionFlowState.Idle;
                Log.WLazy(() => "Server stopped, returning to init state.", this);
                HandleInit();
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {

            //Blocks the host from reacting to its own client connection state changes
            if (_connectionFlowState != ConnectionFlowState.Joining)
                return;

            if (args.ConnectionState == LocalConnectionState.Started)
            {
                Log.DLazy(() => "Client connection started.", this);
                _connectionFlowState = ConnectionFlowState.Idle;
                HandleLobby();
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _connectionFlowState = ConnectionFlowState.Idle;
                Log.WLazy(() => "Client connection stopped, returning to init state.", this);
                HandleInit();
            }
        }

        private void OnDisable()
        {
            if (_serverManager != null)
            {
                _serverManager.OnServerConnectionState -= OnServerConnectionState;
            }

            if (_clientManager != null)
                _clientManager.OnClientConnectionState -= OnClientConnectionState;

            _connectionFlowState = ConnectionFlowState.Idle;
            _pendingRemotePlayerDelta = 0;
            _isInitialized = false;
        }
    }
}
