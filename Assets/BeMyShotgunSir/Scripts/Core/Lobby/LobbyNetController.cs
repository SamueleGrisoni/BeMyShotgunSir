using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;


namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    #region Interfaces


    public interface ILobbyNetController_Command : INetController_Command
    {
        void UpdatePlayerName_ServerRpc(string newName, NetworkConnection conn = null);
        void UpdatePlayerReady_ServerRpc(bool isReady, NetworkConnection conn = null);
        void SelectTeamMate_ServerRpc(int teammateConnectionId, NetworkConnection conn = null);
        void LeaveTeam_ServerRpc(NetworkConnection conn = null);
    }

    public interface ILobbyNetController : INetController, ILobbyNetController_Command, ILobbyNetStateReadWrapped { }

    #endregion

    [RequireComponent(typeof(LobbyManager))]
    [RequireComponent(typeof(LobbyNetStateStore))]
    [RequireComponent(typeof(LobbyClientProjector))]
    public class LobbyNetController : NetController, ILobbyNetController
    {
        private bool _log = true;
        private ServerManager _serverManager;
        private LobbyNetStateStore _netState;
        public ILobbyNetStateRead NetState => _netState;
        private LobbyClientProjector _clientProjection;

        [SerializeField] private NetworkObject _raceManager;
        private NetworkObject _activeRaceManager;

        private void Awake()
        {
            TryGetComponent(out _netState);
            TryGetComponent(out _clientProjection);
            if (_netState == null || _clientProjection == null)
                Log.ELazy(() => "One or more required components are missing on LobbyNetController.", this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _netState.InitSyncValues();
            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _serverManager.OnRemoteConnectionState += OnRemoteConnectionState;

            if (_netState != null)
                _netState.InitSyncValues();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsServerInitialized)
                return;

            NetworkConnection localConnection = GameServices.Instance.NetworkManager.ClientManager.Connection;
            if (localConnection == null)
                return;

            int localId = localConnection.ClientId;
            if (!_netState.TryGetPlayerState(localId, out LobbyPlayerState state))
                return;

            string hostName = "PlayerHost " + localId;
            if (state.PlayerName == hostName)
                return;

            _netState.SetPlayerName(localId, hostName);
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
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        [Server]
        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                if (!_netState.ContainsPlayer(connection.ClientId))
                {
                    string defaultName = "Player " + connection.ClientId;
                    _netState.AddPlayer(connection, defaultName);
                }
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                _netState.RemovePlayer(connection.ClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlayerName_ServerRpc(string newName, NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            int connectionId = conn.ClientId;
            _netState.SetPlayerName(connectionId, newName);
        }

        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlayerReady_ServerRpc(bool isReady, NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            int connectionId = conn.ClientId;
            if (!_netState.TryGetPlayerState(connectionId, out LobbyPlayerState state))
                return;
            if (state.TeamId == -1 && isReady)
            {
                Log.WLazy(() => "Player cannot be ready without selecting a teammate.", this);
                LogMessage_TargetRpc(conn, "You cannot be ready without selecting a teammate.", 1);
                return;
            }
            _netState.SetPlayerReady(connectionId, isReady);

            if (isReady && _netState.CheckAllPlayersReady())
                StartRace();
        }

        [ServerRpc(RequireOwnership = false)]
        public void SelectTeamMate_ServerRpc(int teammateConnectionId, NetworkConnection conn = null)
        {
            if (conn == null)
                return;
            int clientId = conn.ClientId;
            if (_netState.TryGetPlayerState(clientId, out LobbyPlayerState playerState))
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
                if (_netState.TryGetPlayerState(teammateConnectionId, out LobbyPlayerState teammateState))
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
                _netState.AddTeamInfo(currentTeamId, new LobbyTeamInfo(currentTeamId, clientId, teammateConnectionId));
                _netState.SetPlayerTeam(clientId, currentTeamId);
                _netState.SetPlayerTeam(teammateConnectionId, currentTeamId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveTeam_ServerRpc(NetworkConnection conn = null)
        {
            if (conn == null)
                return;
            int clientId = conn.ClientId;
            if (_netState.TryGetPlayerState(clientId, out LobbyPlayerState playerState))
            {
                if (playerState.TeamId != -1)
                {
                    int teamId = playerState.TeamId;
                    if (_netState.TryGetTeamInfo(teamId, out LobbyTeamInfo teamInfo))
                    {
                        int teammateConnectionId = teamInfo.DriverConnectionId == clientId ? teamInfo.ShotgunConnectionId : teamInfo.DriverConnectionId;
                        _netState.RemoveTeam(teamId);
                        _netState.SetPlayerTeam(clientId, -1);
                        if (_netState.ContainsPlayer(teammateConnectionId))
                        {
                            _netState.SetPlayerTeam(teammateConnectionId, -1);
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

        [Server]
        private void StartRace()
        {
            Log.DLazy(() => "All players ready, starting race!", this, _log);
            if (_activeRaceManager == null)
                InitRace();
        }

        [Server]
        private void InitRace()
        {
            if (_raceManager == null)
            {
                Log.ELazy(() => "RaceManager prefab reference is not assigned in the inspector.", this);
                return;
            }

            _activeRaceManager = Instantiate(_raceManager);

            if (_activeRaceManager == null)
            {
                Log.ELazy(() => "Failed to instantiate RaceManager.", this);
                return;
            }

            _activeRaceManager.gameObject.name = _activeRaceManager.gameObject.name.Replace("(Clone)", " Server");
            Spawn(_activeRaceManager);
        }
    }
}
