using System.Collections;
using BeMyShotgunSir.Scripts.Core.Lobby;
using FishNet.Connection;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
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

    [RequireComponent(typeof(SceneCoordinator))]
    public class ConnectionManager : MonoBehaviour
    {
        private bool _isInitialized = false;

        [SerializeField]
        private AppFlowState _currentAppFlow = AppFlowState.Startup;
        private SceneCoordinator _sceneCoordinator;
        private ServerManager _serverManager;
        private ClientManager _clientManager;
        private LobbyManager _lobbyManager;
        [SerializeField] private ConnectionFlowState _connectionFlowState = ConnectionFlowState.Idle;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (GameServices.Instance == null || GameServices.Instance.NetworkManager == null)
            {
                Debug.LogError("ConnectionManager: GameServices or NetworkManager is missing.", this);
                return;
            }

            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _clientManager = GameServices.Instance.NetworkManager.ClientManager;

            if (_serverManager == null || _clientManager == null)
            {
                Debug.LogError("ConnectionManager: ServerManager or ClientManager is missing.", this);
                return;
            }

            _serverManager.OnServerConnectionState += OnServerConnectionState;
            _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _clientManager.OnClientConnectionState += OnClientConnectionState;

            LobbyManager.OnLobbySpawned += HandleLobbySpawned;
            LobbyManager.OnLobbyDespawned += HandleLobbyDespawned;

            TryGetComponent(out _sceneCoordinator);
            ChangeState(AppFlowState.Init);

            _isInitialized = true;
        }
        private void ChangeState(AppFlowState newState)
        {
            _currentAppFlow = newState;

            switch (_currentAppFlow)
            {
                case AppFlowState.Startup:
                    HandleStartup();
                    break;
                case AppFlowState.Init:
                    HandleInit();
                    break;
                case AppFlowState.Lobby:
                    HandleLobby();
                    break;
                default:
                    break;
            }
        }

        private void HandleStartup() { }

        private void HandleInit()
        {
            _serverManager.StopConnection(true);
            _clientManager.StopConnection();
            _sceneCoordinator.LoadInitScene();
        }

        private void HandleLobby() =>
            _sceneCoordinator.LoadLobby();

        private void HandleLobbySpawned(LobbyManager lobby)
        {
            if (_currentAppFlow != AppFlowState.Lobby)
                return;

            _lobbyManager = lobby;
        }
        private void HandleLobbyDespawned()
        {
            if (_currentAppFlow != AppFlowState.Lobby)
                return;

            _lobbyManager = null;
            ChangeState(AppFlowState.Init);
        }

        public void StartHost()
        {
            if (!_isInitialized)
                Initialize();

            _connectionFlowState = ConnectionFlowState.Hosting;
            _serverManager.StartConnection();
            _clientManager.StartConnection();
        }

        public void StartJoin(string ipAddress)
        {
            if (!_isInitialized)
                Initialize();

            _connectionFlowState = ConnectionFlowState.Joining;
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetClientAddress(ipAddress);
            _clientManager.StartConnection();
        }

        public void QuitLobby() =>
            ChangeState(AppFlowState.Init);

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (_connectionFlowState == ConnectionFlowState.Hosting)
                    _connectionFlowState = ConnectionFlowState.Idle;

                StartCoroutine(WaitToLoadLobby());
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _connectionFlowState = ConnectionFlowState.Idle;

                if (_currentAppFlow == AppFlowState.Lobby || _currentAppFlow == AppFlowState.Startup)
                {
                    Debug.LogWarning("ConnectionManager: Server stopped, returning to init state.", this);
                    ChangeState(AppFlowState.Init);
                }
            }
        }

        private void OnRemoteConnectionState(NetworkConnection net, RemoteConnectionStateArgs remote)
        {
            if (remote.ConnectionState == RemoteConnectionState.Started)
            {
                if (_lobbyManager != null)
                    _lobbyManager.AddPlayerToLobby(net);
            }
            if (remote.ConnectionState == RemoteConnectionState.Stopped)
            {
                if (_lobbyManager != null)
                    _lobbyManager.RemovePlayerFromLobby(net);
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            //Blocks the host from reacting to its own client connection state changes
            if (_connectionFlowState != ConnectionFlowState.Joining)
                return;

            if (args.ConnectionState == LocalConnectionState.Started)
            {
                _connectionFlowState = ConnectionFlowState.Idle;
                ChangeState(AppFlowState.Lobby);
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _connectionFlowState = ConnectionFlowState.Idle;
                Debug.LogWarning("ConnectionManager: Join failed or was interrupted.", this);
                ChangeState(AppFlowState.Init);
            }
        }

        private IEnumerator WaitToLoadLobby()
        {
            yield return new WaitForEndOfFrame();
            ChangeState(AppFlowState.Lobby);
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

            LobbyManager.OnLobbySpawned -= HandleLobbySpawned;
            LobbyManager.OnLobbyDespawned -= HandleLobbyDespawned;

            _connectionFlowState = ConnectionFlowState.Idle;
            _isInitialized = false;
        }

    }
}
