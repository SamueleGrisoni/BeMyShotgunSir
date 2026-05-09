using System;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    public class ConnectionManager : MonoBehaviour
    {
        private bool _log = true;
        private bool _isInitialized = false;
        private bool _isHostSession = false;
        private bool _isInLobby = false;

        private ServerManager _serverManager;
        private ClientManager _clientManager;
        [SerializeField] private NetworkObject _lobbyManagerPrefab;
        private NetworkObject _spawnedLobbyManagerInstance;

        private void Awake()
        {
            if (_lobbyManagerPrefab == null)
                Log.ELazy(() => "LobbyManager prefab reference is not assigned in the inspector.", this);

        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            GameServices.Instance.SceneCoordinator.LoadInitScene();

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

            string ip = GetIp(ipAddress);
            ushort portValue = GetPort(ipAddress);
            Log.DLazy(() => $"Setting client address to {ip} on port {portValue}", this, _log);
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetClientAddress(ip);
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetPort(portValue);

            Log.DLazy(() => $"Starting join session", this, _log);
            _clientManager.StartConnection();
        }

        public void QuitLobby()
        {
            if (!_isInLobby)
                return;
            _isInLobby = false;

            Log.DLazy(() => "Quitting lobby.", this, _log);

            if (_isHostSession)
            {
                if (_spawnedLobbyManagerInstance != null)
                {
                    _spawnedLobbyManagerInstance.Despawn();
                    _spawnedLobbyManagerInstance = null;
                }
                _serverManager.StopConnection(true);
            }
            else
            {
                _clientManager.StopConnection();
            }
            GameServices.Instance.SceneCoordinator.LoadInitScene();
        }

        private void EnterLobby()
        {
            if (_isInLobby)
                return;
            _isInLobby = true;

            if (_isHostSession)
            {
                Log.DLazy(() => "Entering lobby as host", this, _log);
                if (_lobbyManagerPrefab != null && _spawnedLobbyManagerInstance == null)
                {
                    NetworkObject instance = Instantiate(_lobbyManagerPrefab);
                    instance.gameObject.name = instance.gameObject.name.Replace("(Clone)", " Server");
                    _serverManager.Spawn(instance);
                }
            }
            else
                Log.DLazy(() => "Entering lobby as client", this, _log);
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                Log.DLazy(() => "Server connection started.", this, _log);
                EnterLobby();
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Log.WLazy(() => "Server connection stopped.", this);
                QuitLobby();
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (_isHostSession)
                return;

            if (args.ConnectionState == LocalConnectionState.Started)
            {
                Log.DLazy(() => "Client connection started.", this, _log);
                EnterLobby();
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Log.WLazy(() => "Client connection stopped.", this);
                QuitLobby();
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

            _isInitialized = false;
        }
    }
}
