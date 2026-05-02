using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.CodeGenerating;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;


namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    #region DataStructures
    public struct LobbyPlayerState
    {
        [ExcludeSerialization]
        public NetworkConnection Connection;
        public int ConnectionId;
        public string PlayerName;
        public int TeamId;
        public bool IsReady;

        public LobbyPlayerState(NetworkConnection connection, string playerName, int teamId = -1, bool isReady = false)
        {
            Connection = connection;
            ConnectionId = connection.ClientId;
            PlayerName = playerName;
            TeamId = teamId;
            IsReady = isReady;
        }

        public override string ToString() =>
            $"LobbyPlayerState: ConnectionId: {ConnectionId}, PlayerName: {PlayerName}, TeamId: {TeamId}, IsReady: {IsReady}";
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
        public override string ToString() =>
            $"LobbyInfo: IP: {LobbyIP}, Max Players: {MaxPlayers}";

    }

    public struct LobbyNetDataSnapshot : ILobbyNetData
    {
        public LobbyInfo LobbyInfo { get; set; }
        public int PlayerCount { get; set; }
        public Dictionary<int, LobbyPlayerState> PlayerStates { get; set; }
        public Dictionary<int, LobbyTeamInfo> TeamInfos { get; set; }

        public LobbyNetDataSnapshot(int playerCount, Dictionary<int, LobbyPlayerState> playerStates, LobbyInfo lobbyInfo, Dictionary<int, LobbyTeamInfo> teamInfos)
        {
            PlayerCount = playerCount;
            PlayerStates = new Dictionary<int, LobbyPlayerState>(playerStates);
            LobbyInfo = lobbyInfo;
            TeamInfos = new Dictionary<int, LobbyTeamInfo>(teamInfos);
        }

        public override string ToString() =>
            $"LobbyNetDataSnapshot: {LobbyInfo}, PlayerCount: {PlayerCount}, PlayerStates: {string.Join(", ", PlayerStates)}, TeamInfos: {string.Join(", ", TeamInfos)}";
    }

    public struct LobbyTeamInfo
    {
        public int TeamId;
        public int DriverConnectionId;
        public int ShotgunConnectionId;

        public LobbyTeamInfo(int teamId, int driverConnectionId = -1, int shotgunConnectionId = -1)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            ShotgunConnectionId = shotgunConnectionId;
        }
        public override string ToString() =>
            $"LobbyTeamInfo: TeamId: {TeamId}, DriverConnectionId: {DriverConnectionId}, ShotgunConnectionId: {ShotgunConnectionId}";
    }
    #endregion

    #region LobbyNetControllerInterfaces
    public interface ILobbyNetController_Command : INetController_Command
    {
        void HandleRefresh();
        void UpdatePlayerName_ServerRpc(string newName, NetworkConnection conn = null);
        void UpdatePlayerReady_ServerRpc(bool isReady, NetworkConnection conn = null);
        void SelectTeamMate_ServerRpc(int teammateConnectionId, NetworkConnection conn = null);
        void LeaveTeam_ServerRpc(NetworkConnection conn = null);
    }
    public interface ILobbyNetController_RaceNetController
    {
        LobbyInfo LobbyInfo { get; }
        IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos { get; }
        int PlayerCount { get; }
    }
    public interface ILobbyNetController : INetController, ILobbyNetController_Command, ILobbyNetController_RaceNetController { }

    #endregion

    [RequireComponent(typeof(ILobbyManager))]
    public class LobbyNetController : NetController, ILobbyNetController
    {
        private bool _log = true;

        #region Lifecycle
        public static event Action<ILobbyNetController> OnLobbyNetControllerReady;
        public static event Action OnLobbyNetControllerDespawned;
        private ServerManager _serverManager;
        private ILobbyManager_NetController _lobbyManager;

        [SerializeField] private NetworkObject _raceManager;
        private NetworkObject _activeRaceManager;

        private void OnEnable()
        {
            LobbyManager.OnLobbyManagerStarted += OnLobbyManagerStarted;
            RaceNetController.OnRaceNetControllerReady += OnRaceNetControllerReady;
        }

        private void OnRaceNetControllerReady(IRaceNetController controller) => controller.SetLobbyNetController(this);

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _lobbyInfo.OnChange += OnLobbyInfoChanged;
            _playerStates.OnChange += OnPlayerStatesChanged;
            _teamInfos.OnChange += OnTeamInfosChanged;
            _playerCount.OnChange += OnPlayerCountChanged;
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

            Log.DLazy(() => "LobbyNetController is ready!", this, _log);
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
            if (!_playerStates.TryGetValue(localId, out LobbyPlayerState state))
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
            Log.DLazy(() => "LobbyNetController despawned from the network.", this, _log);
            OnLobbyNetControllerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();


        private void UnsubscribeEvents()
        {
            _lobbyInfo.OnChange -= OnLobbyInfoChanged;
            _playerCount.OnChange -= OnPlayerCountChanged;
            _playerStates.OnChange -= OnPlayerStatesChanged;
            _teamInfos.OnChange -= OnTeamInfosChanged;
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
                    _playerStates[connection.ClientId] = new LobbyPlayerState(connection, defaultName);
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
            Log.DLazy(() => "All players ready, starting race!", this, _log);
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
                _lobbyManager.InitNetData_Response(new LobbyNetDataSnapshot(_playerCount.Value, _playerStates.Collection, _lobbyInfo.Value, _teamInfos.Collection));
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

            var snapshot = new LobbyNetDataSnapshot(_playerCount.Value, _playerStates.Collection, _lobbyInfo.Value, _teamInfos.Collection);

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
        public LobbyInfo LobbyInfo => _lobbyInfo.Value;
        private void OnLobbyInfoChanged(LobbyInfo prev, LobbyInfo next, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            _lobbyManager.SetLobbyInfo_Response(next);
        }
        #endregion

        #region PlayerStates
        private readonly SyncDictionary<int, LobbyPlayerState> _playerStates = new();
        public IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates => _playerStates;
        private void OnPlayerStatesChanged(SyncDictionaryOperation op, int key, LobbyPlayerState value, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    _lobbyManager.SetPlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Set:
                    _lobbyManager.SetPlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Remove:
                    _lobbyManager.SetPlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Clear:
                    _lobbyManager.SetPlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Complete:
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
            LobbyPlayerState state = _playerStates[connectionId];
            state.PlayerName = newName;
            _playerStates[connectionId] = state;
        }

        [ServerRpc(RequireOwnership = false)] //TODO
        public void UpdatePlayerReady_ServerRpc(bool isReady, NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            int connectionId = conn.ClientId;
            LobbyPlayerState state = _playerStates[connectionId];
            if (state.TeamId == -1 && isReady)
            {
                Log.ELazy(() => "Player cannot be ready without selecting a teammate.", this);
                LogMessage_TargetRpc(conn, "You cannot be ready without selecting a teammate.", 1);
                return;
            }
            state.IsReady = isReady;
            _playerStates[connectionId] = state;

            if (isReady)
                CheckAllPlayersReady();
        }

        [Server]
        private void CheckAllPlayersReady()
        {
            bool allReady = true;
            foreach (KeyValuePair<int, LobbyPlayerState> kvp in _playerStates.Collection)
            {
                if (!kvp.Value.IsReady)
                {
                    allReady = false;
                    break;
                }
            }
            if (allReady)
                StartRace();
        }
        #endregion

        #region TeamInfo
        private readonly SyncDictionary<int, LobbyTeamInfo> _teamInfos = new();
        public IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos => _teamInfos;
        private void OnTeamInfosChanged(SyncDictionaryOperation op, int key, LobbyTeamInfo value, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            var teamInfos = new Dictionary<int, LobbyTeamInfo>(_teamInfos.Collection);
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    _lobbyManager.SetTeamInfos_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Set:
                    _lobbyManager.SetTeamInfos_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Remove:
                    _lobbyManager.SetTeamInfos_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Clear:
                    _lobbyManager.SetTeamInfos_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
        }

        [ServerRpc(RequireOwnership = false)] //TODO test it
        public void SelectTeamMate_ServerRpc(int teammateConnectionId, NetworkConnection conn = null)
        {
            if (conn == null)
                return;
            int clientId = conn.ClientId;
            if (_playerStates.TryGetValue(clientId, out LobbyPlayerState playerState))
            {
                if (teammateConnectionId == clientId)
                {
                    LogMessage_TargetRpc(conn, "You cannot select yourself as a teammate.", 1);
                    return;
                }
                if (playerState.TeamId != -1)
                {
                    LogMessage_TargetRpc(conn, "You are already in a team.", 1);
                    return;
                }
                if (_playerStates.TryGetValue(teammateConnectionId, out LobbyPlayerState teammateState))
                {
                    if (teammateState.TeamId != -1)
                    {
                        LogMessage_TargetRpc(conn, "Selected teammate is already in a team.", 1);
                        return;
                    }
                }
                else
                {
                    LogMessage_TargetRpc(conn, "Selected teammate connection ID does not exist.", 2);
                    return;
                }

                int currentTeamId = TeamIdGenerator.GetTeamId(clientId, teammateConnectionId);
                _teamInfos[currentTeamId] = new LobbyTeamInfo(currentTeamId, clientId, teammateConnectionId);
                _playerStates[clientId] = new LobbyPlayerState(conn, playerState.PlayerName, currentTeamId, false);
                _playerStates[teammateConnectionId] = new LobbyPlayerState(_playerStates[teammateConnectionId].Connection, teammateState.PlayerName, currentTeamId, false);
            }
        }

        [ServerRpc(RequireOwnership = false)] //TODO test it
        public void LeaveTeam_ServerRpc(NetworkConnection conn = null)
        {
            if (conn == null)
                return;
            int clientId = conn.ClientId;
            if (_playerStates.TryGetValue(clientId, out LobbyPlayerState playerState))
            {
                if (playerState.TeamId != -1)
                {
                    int teamId = playerState.TeamId;
                    if (_teamInfos.TryGetValue(teamId, out LobbyTeamInfo teamInfo))
                    {
                        int teammateConnectionId = teamInfo.DriverConnectionId == clientId ? teamInfo.ShotgunConnectionId : teamInfo.DriverConnectionId;
                        _teamInfos.Remove(teamId);
                        _playerStates[clientId] = new LobbyPlayerState(conn, playerState.PlayerName, -1, false);
                        if (_playerStates.ContainsKey(teammateConnectionId))
                        {
                            LobbyPlayerState teammateState = _playerStates[teammateConnectionId];
                            _playerStates[teammateConnectionId] = new LobbyPlayerState(teammateState.Connection, teammateState.PlayerName, -1, false);
                        }
                        else
                        {
                            LogMessage_TargetRpc(conn, "Teammate connection ID does not exist.", 2);
                            return;
                        }
                    }
                    else
                    {
                        LogMessage_TargetRpc(conn, "Player team info not found.", 2);
                        return;
                    }
                }
                else
                {
                    LogMessage_TargetRpc(conn, "Player is not in a team.", 1);
                    return;
                }
            }
        }
        #endregion

        #region PlayerCount
        private readonly SyncVar<int> _playerCount = new(0);
        public int PlayerCount => _playerCount.Value;
        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (asServer || _lobbyManager == null)
                return;
            _lobbyManager.SetPlayerCount_Response(prev, next);
        }
        #endregion
    }
}
