using System.Collections;
using BeMyShotgunSir.Scripts.Core.Lobby;
using FishNet.Connection;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core
{
    public enum InitManagerState
    {
        Startup,
        Init,
        Lobby
    }

    [RequireComponent(typeof(SceneLoader))]
    public class ConnectionManager : MonoBehaviour
    {
        private static ConnectionManager _instance;
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        [SerializeField]
        private InitManagerState _currentState = InitManagerState.Startup;
        private SceneLoader _sceneLoader;
        private ServerManager _serverManager;
        private ClientManager _clientManager;
        private LobbyManager _lobbyManager;

        private static bool _isInitialized = false;
        public void Initialize()
        {
            if (_isInitialized)
                return;

            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _clientManager = GameServices.Instance.NetworkManager.ClientManager;

            if (_serverManager != null)
            {
                _serverManager.OnServerConnectionState += OnServerConnectionState;
                _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;
            }

            LobbyManager.OnLobbySpawned += HandleLobbySpawned;
            LobbyManager.OnLobbyDespawned += HandleLobbyDespawned;

            TryGetComponent(out _sceneLoader);
            ChangeState(InitManagerState.Init);

            _isInitialized = true;
        }
        private void HandleLobbySpawned(LobbyManager lobby) => _lobbyManager = lobby;
        private void HandleLobbyDespawned() => _lobbyManager = null;

        public void ChangeState(InitManagerState newState)
        {
            InitManagerState prevState = _currentState;
            _currentState = newState;

            switch (_currentState)
            {
                case InitManagerState.Startup:
                    HandleStartup();
                    break;
                case InitManagerState.Init:
                    HandleInit();
                    break;
                case InitManagerState.Lobby:
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
            _sceneLoader.LoadInitMenuScene();
        }

        private void HandleLobby() => _sceneLoader.LoadLobbyScene();

        public void StartHost()
        {
            _serverManager.StartConnection();
            _clientManager.StartConnection();
        }

        public void StartJoin(string ipAddress)
        {
            GameServices.Instance.NetworkManager.TransportManager.Transport.SetClientAddress(ipAddress);
            _clientManager.StartConnection();
            ChangeState(InitManagerState.Lobby);
        }

        private void OnDisable()
        {
            if (_serverManager != null)
            {
                _serverManager.OnServerConnectionState -= OnServerConnectionState;
                _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            }

            LobbyManager.OnLobbySpawned -= HandleLobbySpawned;
            LobbyManager.OnLobbyDespawned -= HandleLobbyDespawned;
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                StartCoroutine(WaitToLoadLobby());
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

        private IEnumerator WaitToLoadLobby()
        {
            yield return new WaitForEndOfFrame();
            ChangeState(InitManagerState.Lobby);
        }

    }
}
