using System;
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
        private bool _log = true;
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

            Log.DLazy(() => "ConnectionManager initialized.", this, _log);

            HandleInit();

            _isInitialized = true;
        }

        private string GetIp(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || string.Equals(ipAddress, "localhost", StringComparison.OrdinalIgnoreCase))
                return "127.0.0.1";
            else if (ipAddress.Split(':').Length > 1) //Handles case where user inputs "ip:port"
                return ipAddress.Split(':')[0];
            else
                return ipAddress;
        }

        private ushort GetPort(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || string.Equals(ipAddress, "localhost", StringComparison.OrdinalIgnoreCase))
                return 7777;
            else if (ipAddress.Split(':').Length > 1) //Handles case where user inputs "ip:port"
            {
                if (ushort.TryParse(ipAddress.Split(':')[1], out ushort port))
                    return port;
                else
                {
                    Log.WLazy(() => $"Failed to parse port from IP address input '{ipAddress}', defaulting to 7777.", this);
                    return 7777;
                }
            }
            else
                return 7777;
        }

        public void StartHost(string ipAddress)
        {
            if (!_isInitialized)
                Initialize();

            _isHostSession = true;
            _connectionFlowState = ConnectionFlowState.Hosting;

            string ip = GetIp(ipAddress);
            ushort portValue = GetPort(ipAddress);
            Log.DLazy(() => $"Setting server bind address to {ip} on port {portValue}", this, _log);
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetServerBindAddress(ip, IPAddressType.IPv4);
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetPort(portValue);

            Log.DLazy(() => "Starting host session.", this, _log);
            _serverManager.StartConnection();
            _clientManager.StartConnection();
        }

        public void StartJoin(string ipAddress)
        {
            if (!_isInitialized)
                Initialize();

            _isHostSession = false;
            _connectionFlowState = ConnectionFlowState.Joining;

            string ip = GetIp(ipAddress);
            ushort portValue = GetPort(ipAddress);
            Log.DLazy(() => $"Setting client address to {ip}.", this, _log);
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetClientAddress(ip);

            Log.DLazy(() => $"Starting join session to {ip}.", this, _log);
            _clientManager.StartConnection();
        }

        public void QuitLobby()
        {
            Log.DLazy(() => "Quitting lobby", this, _log);
            HandleInit();
        }

        private void HandleStartup()
        {
            if (_currentAppFlow == AppFlowState.Startup)
                return;

            Log.DLazy(() => "Start Up State", this, _log);
            _currentAppFlow = AppFlowState.Startup;
        }

        private void HandleInit()
        {

            if (_spawnedLobbyManagerInstance != null)
            {
                _spawnedLobbyManagerInstance.Despawn();
                _spawnedLobbyManagerInstance = null;
            }

            Log.DLazy(() => "Stopping all connections", this, _log);

            _clientManager.StopConnection();
            _serverManager.StopConnection(true);

            if (_currentAppFlow == AppFlowState.Init)
                return;

            Log.DLazy(() => "Init State", this, _log);
            _currentAppFlow = AppFlowState.Init;

            GameServices.Instance.SceneCoordinator.LoadInitScene();
        }

        private void HandleLobby()
        {
            if (_currentAppFlow == AppFlowState.Lobby)
                return;

            Log.DLazy(() => "Lobby State", this, _log);
            _currentAppFlow = AppFlowState.Lobby;

            if (!_isHostSession)
                return;
            //NOTE host only operations below this

            if (_lobbyManagerPrefab != null && _spawnedLobbyManagerInstance == null)
            {
                NetworkObject instance = Instantiate(_lobbyManagerPrefab);
                instance.gameObject.name = instance.gameObject.name.Replace("(Clone)", " Server");
                _serverManager.Spawn(instance);
            }
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (_connectionFlowState == ConnectionFlowState.Hosting)
                    _connectionFlowState = ConnectionFlowState.Idle;

                Log.DLazy(() => "Server connection started.", this, _log);
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
                Log.DLazy(() => "Client connection started.", this, _log);
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
