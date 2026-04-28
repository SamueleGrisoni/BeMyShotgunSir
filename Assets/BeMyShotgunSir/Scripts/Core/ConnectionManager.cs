using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
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

    public class ConnectionManager : MonoBehaviour
    {
        private bool _isInitialized = false;
        private bool _isHostSession = false;

        [SerializeField]
        private AppFlowState _currentAppFlow = AppFlowState.Startup;
        private ServerManager _serverManager;
        private ClientManager _clientManager;
        private LobbyManager _lobbyManager;
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
            _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _clientManager.OnClientConnectionState += OnClientConnectionState;

            LobbyManager.OnLobbyManagerSpawned += HandleLobbyManagerSpawned;
            LobbyManager.OnLobbyManagerDespawned += HandleLobbyManagerDespawned;

            HandleInit();

            _isInitialized = true;
        }

        public void StartHost()
        {
            if (!_isInitialized)
                Initialize();

            _isHostSession = true;

            _connectionFlowState = ConnectionFlowState.Hosting;
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

            if (_lobbyManagerPrefab != null && _spawnedLobbyManagerInstance == null)
            {
                NetworkObject instance = Instantiate(_lobbyManagerPrefab);
                instance.gameObject.name = instance.gameObject.name.Replace("(Clone)", "");
                _serverManager.Spawn(instance);
                _spawnedLobbyManagerInstance = instance;
            }
        }

        private void HandleLobbyManagerSpawned(LobbyManager lobby)
        {
            if (lobby == null)
                return;

            _lobbyManager = lobby;

            if (_isHostSession)
            {
                if (_pendingRemotePlayerDelta != 0)
                {
                    _lobbyManager.AdjustPlayerCount(_pendingRemotePlayerDelta);
                    _pendingRemotePlayerDelta = 0;
                }
            }
        }
        private void HandleLobbyManagerDespawned()
        {
            _lobbyManager = null;
            HandleInit();
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (_connectionFlowState == ConnectionFlowState.Hosting)
                    _connectionFlowState = ConnectionFlowState.Idle;

                HandleLobby();
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _connectionFlowState = ConnectionFlowState.Idle;

                if (_currentAppFlow == AppFlowState.Lobby || _currentAppFlow == AppFlowState.Startup)
                {
                    Debug.LogWarning("Server stopped, returning to init state.", this);
                    HandleInit();
                }
            }
        }

        private void OnRemoteConnectionState(NetworkConnection net, RemoteConnectionStateArgs remote)
        {
            LobbyManager mng = GameServices.Instance.LobbyManager;
            if (mng == null)
            {
                if (remote.ConnectionState == RemoteConnectionState.Started)
                    _pendingRemotePlayerDelta++;
                else if (remote.ConnectionState == RemoteConnectionState.Stopped)
                    _pendingRemotePlayerDelta--;
                return;
            }
            else if (_pendingRemotePlayerDelta != 0)
                mng.AdjustPlayerCount(_pendingRemotePlayerDelta);
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            //Blocks the host from reacting to its own client connection state changes
            if (_connectionFlowState != ConnectionFlowState.Joining)
                return;

            if (args.ConnectionState == LocalConnectionState.Started)
            {
                _connectionFlowState = ConnectionFlowState.Idle;
                HandleLobby();
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _connectionFlowState = ConnectionFlowState.Idle;
                Debug.LogWarning("Join failed or was interrupted.", this);
                HandleInit();
            }
        }

        private void OnDisable()
        {
            if (_serverManager != null)
            {
                _serverManager.OnServerConnectionState -= OnServerConnectionState;
                _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            }

            if (_clientManager != null)
                _clientManager.OnClientConnectionState -= OnClientConnectionState;

            LobbyManager.OnLobbyManagerSpawned -= HandleLobbyManagerSpawned;
            LobbyManager.OnLobbyManagerDespawned -= HandleLobbyManagerDespawned;

            _connectionFlowState = ConnectionFlowState.Idle;
            _pendingRemotePlayerDelta = 0;
            _isInitialized = false;
        }

    }
}
