using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    #region DataStructures
    public struct PlayerLobbyState
    {
        public int ConnectionId;
        public string PlayerName;
        public bool IsReady;

        public PlayerLobbyState(NetworkConnection connection, string playerName)
        {
            ConnectionId = connection.ClientId;
            PlayerName = playerName;
            IsReady = false;
        }
    }

    public struct LobbyInfo
    {
        public int MaxPlayers;
        public string LobbyIP;
        public LobbyInfo(string lobbyIP = "<IP>:<Port>")
        {
            LobbyIP = lobbyIP;
            //CONST VALUES DOWN
            MaxPlayers = 4;
        }
    }

    public struct LobbyNetDataSnapshot : ILobbyNetData
    {
        public LobbyInfo LobbyInfo { get; set; }
        public int PlayerCount { get; set; }
        public Dictionary<int, PlayerLobbyState> PlayerStates { get; set; }

        public LobbyNetDataSnapshot(int playerCount, Dictionary<int, PlayerLobbyState> playerStates, LobbyInfo lobbyInfo)
        {
            PlayerCount = playerCount;
            PlayerStates = new Dictionary<int, PlayerLobbyState>(playerStates);
            LobbyInfo = lobbyInfo;
        }
    }
    #endregion

    #region LobbyNetControllerInterfaces
    public interface ILobbyNetController_Command : INetController_Command
    {
        void HandleRefresh();
        void UpdatePlayerName_ServerRpc(string newName, NetworkConnection conn = null);
        void UpdatePlayerReady_ServerRpc(bool isReady, NetworkConnection conn = null);
    }
    public interface ILobbyNetController : INetController, ILobbyNetController_Command { }

    #endregion

    [RequireComponent(typeof(ILobbyManager))]
    public class LobbyNetController : NetController, ILobbyNetController
    {
        #region Lifecycle
        public static event Action<ILobbyNetController> OnLobbyNetControllerReady;
        public static event Action OnLobbyNetControllerDespawned;
        private ServerManager _serverManager;
        private ILobbyManager_NetController _lobbyManager;

        [SerializeField] private NetworkObject _raceManager;
        private NetworkObject _activeRaceManager;

        private void OnEnable() =>
            LobbyManager.OnLobbyManagerStarted += OnLobbyManagerStarted;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // _lobbyIP.OnChange += OnLobbyIPChanged;
            _playerCount.OnChange += OnPlayerCountChanged;
            _playerStates.OnChange += OnPlayerStatesChanged;
        }

        private void OnLobbyManagerStarted(ILobbyManager manager)
        {
            if (manager is not ILobbyManager_NetController manager_NetController)
                return;

            if (_lobbyManager != null)
            {
                Log.ELazy(() => "LobbyManager reference is already set. Multiple LobbyManagers are not supported.", this);
                return;
            }

            _lobbyManager = manager_NetController;

            Log.DLazy(() => "LobbyNetController is ready!", this);
            OnLobbyNetControllerReady?.Invoke(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;

            InitSyncValues();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            //NOTE host operations must be written below this check, otherwise the host will execute them twice (once as server, once as client)
            if (!IsServerInitialized)
                return;

            NetworkConnection localConnection = GameServices.Instance.NetworkManager.ClientManager.Connection;
            if (localConnection == null)
                return;

            int localId = localConnection.ClientId;
            if (!_playerStates.TryGetValue(localId, out PlayerLobbyState state))
                return;

            string hostName = "PlayerHost " + localId;
            if (state.PlayerName == hostName)
                return;

            state.PlayerName = hostName;
            _playerStates[localId] = state;
        }

        private void InitSyncValues()
        {
            _playerCount.Value = 0;
            string lobbyIP = GameServices.Instance.NetworkManager.TransportManager.Transport.GetClientAddress() + ":" + GameServices.Instance.NetworkManager.TransportManager.Transport.GetPort();
            _lobbyInfo.Value = new LobbyInfo(lobbyIP);
            _playerStates.Collection.Clear();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
            Log.DLazy(() => "LobbyNetController despawned from the network.", this);
            OnLobbyNetControllerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();


        private void UnsubscribeEvents()
        {
            _lobbyInfo.OnChange -= OnLobbyInfoChanged;
            _playerCount.OnChange -= OnPlayerCountChanged;
            _playerStates.OnChange -= OnPlayerStatesChanged;

            LobbyManager.OnLobbyManagerStarted -= OnLobbyManagerStarted;

            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }
        #endregion

        [Server]
        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                if (!_playerStates.ContainsKey(connection.ClientId))
                {
                    _playerCount.Value++;
                    string defaultName = "Player " + connection.ClientId;
                    _playerStates[connection.ClientId] = new PlayerLobbyState(connection, defaultName);
                }
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                _playerCount.Value--;
                if (_playerStates.ContainsKey(connection.ClientId))
                    _playerStates.Remove(connection.ClientId);
            }
        }

        [Server]
        private void StartRace()
        {
            Log.DLazy(() => "All players ready, starting race!", this);
            if (_activeRaceManager != null)
            {
                Log.ELazy(() => "Active RaceManager already exists.", this);
                return;
            }
            if (_raceManager == null)
            {
                Log.ELazy(() => "RaceManager prefab reference is not assigned in the inspector.", this);
                return;
            }

            _activeRaceManager = Instantiate(_raceManager);

            if (_activeRaceManager == null)
            {
                Log.ELazy(() => "RaceManager prefab does not have a NetworkObject component.", this);
                Destroy(_activeRaceManager);
                return;
            }

            _activeRaceManager.gameObject.name = _activeRaceManager.gameObject.name.Replace("(Clone)", " Server");
            Spawn(_activeRaceManager);
        }

        #region Snapshot
        public void HandleRefresh()
        {
            if (IsController)
            {
                _lobbyManager.InitNetData_Response(new LobbyNetDataSnapshot(_playerCount.Value, _playerStates.Collection, _lobbyInfo.Value));
                return; // without this the host would also call RequestNetDataSnapshot_ServerRpc
            }
            if (IsClientInitialized)
                RequestNetDataSnapshot_ServerRpc();
        }

        /// <summary>
        /// When a client starts, it requests the current lobby data snapshot from the server to initialize its local lobby state. <br/>
        /// This ensures that late-joining clients have the correct lobby information upon joining.
        /// </summary>
        /// <param name="conn"></param>
        [ServerRpc(RequireOwnership = false)]
        private void RequestNetDataSnapshot_ServerRpc(NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            var snapshot = new LobbyNetDataSnapshot(_playerCount.Value, _playerStates.Collection, _lobbyInfo.Value);

            InitNetData_TargetRpc(conn, snapshot);
        }

        /// <summary>
        /// Sends the current lobby data snapshot to the specified client connection. <br/>
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="snapshot"></param>
        [TargetRpc]
        private void InitNetData_TargetRpc(NetworkConnection conn, LobbyNetDataSnapshot snapshot)
        {
            if (conn == null)
                return;
            _lobbyManager.InitNetData_Response(snapshot);
        }
        #endregion

        #region LobbyInfo
        private readonly SyncVar<LobbyInfo> _lobbyInfo = new(default);
        private void OnLobbyInfoChanged(LobbyInfo prev, LobbyInfo next, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            _lobbyManager.SetLobbyInfo_Response(next);
        }
        #endregion

        #region PlayerStates
        private readonly SyncDictionary<int, PlayerLobbyState> _playerStates = new();
        private void OnPlayerStatesChanged(SyncDictionaryOperation op, int key, PlayerLobbyState value, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            var playerStates = new Dictionary<int, PlayerLobbyState>(_playerStates.Collection);
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                case SyncDictionaryOperation.Remove:
                case SyncDictionaryOperation.Clear:
                case SyncDictionaryOperation.Complete:
                    _lobbyManager.SetPlayerStates_Response(op, key, value);
                    break;
                default:
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)] //TODO
        public void UpdatePlayerName_ServerRpc(string newName, NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            int connectionId = conn.ClientId;
            PlayerLobbyState state = _playerStates[connectionId];
            state.PlayerName = newName;
            _playerStates[connectionId] = state;
        }

        [ServerRpc(RequireOwnership = false)] //TODO
        public void UpdatePlayerReady_ServerRpc(bool isReady, NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            int connectionId = conn.ClientId;
            PlayerLobbyState state = _playerStates[connectionId];
            state.IsReady = isReady;
            _playerStates[connectionId] = state;

            if (isReady)
            {
                bool allReady = true;
                foreach (KeyValuePair<int, PlayerLobbyState> kvp in _playerStates.Collection)
                {
                    if (!kvp.Value.IsReady)
                    {
                        allReady = false;
                        break;
                    }
                }
                if (allReady)
                {
                    StartRace();
                }
            }
        }
        #endregion

        #region PlayerCount
        private readonly SyncVar<int> _playerCount = new(0);
        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            _lobbyManager.SetPlayerCount_Response(prev, next);
        }
        #endregion
    }
}
