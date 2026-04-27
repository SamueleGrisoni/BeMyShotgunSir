using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
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
        public LobbyInfo(bool _)
        {
            MaxPlayers = 4;
        }
    }

    public struct LobbyNetDataSnapshot : ILobbyNetData
    {
        public string LobbyIP { get; set; }
        public LobbyInfo LobbyInfo { get; set; }
        public int PlayerCount { get; set; }
        public Dictionary<int, PlayerLobbyState> PlayerStates { get; set; }

        public LobbyNetDataSnapshot(string lobbyIP, int playerCount, Dictionary<int, PlayerLobbyState> playerStates, LobbyInfo lobbyInfo)
        {
            LobbyIP = lobbyIP;
            PlayerCount = playerCount;
            PlayerStates = new Dictionary<int, PlayerLobbyState>(playerStates);
            LobbyInfo = lobbyInfo;
        }
    }

    public class LobbyNetController : NetworkBehaviour
    {
        private ServerManager _serverManager;
        private LobbyManager _lobbyManager;
        public event Action OnLobbyNetControllerSpawned;
        public event Action OnLobbyNetControllerDespawned;

        private void Awake()
        {
            if (!TryGetComponent(out _lobbyManager))
                Debug.LogError("LobbyNetController: LobbyManager not found as parent!", this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _lobbyIP.OnChange += OnLobbyIPChanged;
            _playerCount.OnChange += OnPlayerCountChanged;
            _playerStates.OnChange += OnPlayerStatesChanged;

            OnLobbyNetControllerSpawned?.Invoke();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _serverManager.OnRemoteConnectionState += HandleRemoteConnectionState;

            GameServices.Instance.SceneCoordinator.LoadLobbyScene();

            InitSyncValues();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Solo host: il server è l'unico che può scrivere SyncDictionary.
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
            _lobbyIP.Value = GameServices.Instance.NetworkManager.TransportManager.Transport.GetClientAddress() + ":" + GameServices.Instance.NetworkManager.TransportManager.Transport.GetPort();
            _lobbyInfo.Value = new LobbyInfo(true);
            _playerStates.Collection.Clear();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _serverManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _lobbyIP.OnChange -= OnLobbyIPChanged;
            _playerCount.OnChange -= OnPlayerCountChanged;
            _playerStates.OnChange -= OnPlayerStatesChanged;
            OnLobbyNetControllerDespawned?.Invoke();
        }

        public void HandleRefresh()
        {
            if (IsController)
            {
                _lobbyManager.InitNetData_Response(new LobbyNetDataSnapshot(_lobbyIP.Value, _playerCount.Value, _playerStates.Collection, _lobbyInfo.Value));
                return; // without this the host would also call RequestNetDataSnapshot_ServerRpc
            }
            if (IsClientInitialized)
                RequestNetDataSnapshot_ServerRpc();
        }


        [Server]
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
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


        #region LobbyConnectionManagement
        /// <summary>
        /// Adjusts the player count in the lobby. This should be called on the server when the player count needs to be updated. <br/>
        /// The delta parameter indicates how much to adjust the player count by (positive to add players, negative to remove players). <br/>
        /// <b>Important:</b> <br/>
        /// LobbyManager and LobbyNetController could still be null when a new connection is established, or when a connection is lost. This method allows to adjust the player count when they become available.
        /// </summary>
        /// <param name="delta"></param>
        [Server]
        public void AdjustPlayerCount(int delta)
        {
            int newValue = _playerCount.Value + delta;
            _playerCount.Value = Mathf.Max(0, newValue);
        }
        #endregion

        /// <summary>
        /// When a client starts, it requests the current lobby data snapshot from the server to initialize its local lobby state. <br/>
        /// This ensures that late-joining clients have the correct lobby information upon joining.
        /// </summary>
        /// <param name="conn"></param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestNetDataSnapshot_ServerRpc(NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            var snapshot = new LobbyNetDataSnapshot(_lobbyIP.Value, _playerCount.Value, _playerStates.Collection, _lobbyInfo.Value);

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


        private readonly SyncVar<LobbyInfo> _lobbyInfo = new(default);
        public void OnLobbyInfoChanged(LobbyInfo prev, LobbyInfo next, bool asServer) =>
            _lobbyManager.SetLobbyInfo_Response(next);


        private readonly SyncDictionary<int, PlayerLobbyState> _playerStates = new();

        private void OnPlayerStatesChanged(SyncDictionaryOperation op, int key, PlayerLobbyState value, bool asServer)
        {
            if (asServer)
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
        }

        private readonly SyncVar<string> _lobbyIP = new("Not connected");
        private void OnLobbyIPChanged(string prev, string next, bool asServer)
        {
            if (asServer)
                return;
            _lobbyManager.SetLobbyIP_Response(next);
        }


        private readonly SyncVar<int> _playerCount = new(0);
        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (asServer)
                return;
            _lobbyManager.SetPlayerCount_Response(prev, next);
        }
    }
}
