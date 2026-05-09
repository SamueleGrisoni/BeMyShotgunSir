using System.Collections.Generic;
using FishNet.CodeGenerating;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    #region DataStructures

    public struct LobbyPlayerState
    {
        [ExcludeSerialization]
        public NetworkConnection Connection;
        public int ConnectionId;
        public string PlayerName;
        public int? TeamId;
        public bool IsReady;

        public LobbyPlayerState(NetworkConnection connection, string playerName, int? teamId = null, bool isReady = false)
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
        public LobbyInfo(string lobbyIP = null)
        {
            LobbyIP = lobbyIP ?? BMMSDefaults.IP_PLACEHOLDER;
            MaxPlayers = BMMSDefaults.MAX_PLAYERS;
        }
        public override string ToString() =>
            $"LobbyInfo: IP: {LobbyIP}, Max Players: {MaxPlayers}";
    }

    public struct LobbyTeamInfo
    {
        public int TeamId;
        public int DriverConnectionId;
        public int ShotgunConnectionId;

        public LobbyTeamInfo(int teamId, int driverConnectionId, int shotgunConnectionId)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            ShotgunConnectionId = shotgunConnectionId;
        }
        public override string ToString() =>
            $"LobbyTeamInfo: TeamId: {TeamId}, DriverConnectionId: {DriverConnectionId}, ShotgunConnectionId: {ShotgunConnectionId}";
    }

    #endregion

    #region Interfaces

    public interface ILobbyNetStateRead
    {
        LobbyInfo LobbyInfo { get; }
        int PlayerCount { get; }
        IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos { get; }
        bool TryGetPlayerState(int clientId, out LobbyPlayerState state);
        bool TryGetTeamInfo(int teamId, out LobbyTeamInfo info);
        bool CheckAllPlayersReady();
        bool ContainsPlayer(int clientId);
    }

    public interface ILobbyNetStateSubscribe : ILobbyNetStateRead
    {
        SyncVar<LobbyInfo> LobbyInfo_Sub { get; }
        SyncDictionary<int, LobbyPlayerState> PlayerStates_Sub { get; }
        SyncDictionary<int, LobbyTeamInfo> TeamInfos_Sub { get; }
        SyncVar<int> PlayerCount_Sub { get; }
    }

    public interface ILobbyNetStateStore : ILobbyNetStateSubscribe { }

    #endregion

    public sealed class LobbyNetStateStore : NetworkBehaviour, ILobbyNetStateStore
    {
        // utility
        private bool _log = true;

        //Networked state
        /// <summary> synced </summary>
        private readonly SyncVar<LobbyInfo> _lobbyInfo = new(default);
        /// <summary> synced </summary>
        private readonly SyncDictionary<int, LobbyPlayerState> _playerStates = new();
        /// <summary> synced </summary>
        private readonly SyncDictionary<int, LobbyTeamInfo> _teamInfos = new();
        /// <summary> synced </summary>
        private readonly SyncVar<int> _playerCount = new(0);

        //Subscriber accessors (Projectors)
        SyncVar<LobbyInfo> ILobbyNetStateSubscribe.LobbyInfo_Sub => _lobbyInfo;
        SyncDictionary<int, LobbyPlayerState> ILobbyNetStateSubscribe.PlayerStates_Sub => _playerStates;
        SyncDictionary<int, LobbyTeamInfo> ILobbyNetStateSubscribe.TeamInfos_Sub => _teamInfos;
        SyncVar<int> ILobbyNetStateSubscribe.PlayerCount_Sub => _playerCount;

        //State Read-only accessors
        public LobbyInfo LobbyInfo => _lobbyInfo.Value;
        public IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates => _playerStates;
        public IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos => _teamInfos;
        public int PlayerCount => _playerCount.Value;

        [Server]
        public void InitSyncValues()
        {
            _playerCount.Value = 0;
            string lobbyIP = GameServices.Instance.NetworkManager.TransportManager.Transport.GetServerBindAddress(FishNet.Transporting.IPAddressType.IPv4) + ":" + GameServices.Instance.NetworkManager.TransportManager.Transport.GetPort();
            _lobbyInfo.Value = new LobbyInfo(lobbyIP);
            _playerStates.Collection.Clear();
            _teamInfos.Collection.Clear();
        }

        public bool TryGetPlayerState(int clientId, out LobbyPlayerState state) => _playerStates.TryGetValue(clientId, out state);

        [Server]
        public void AddPlayer(NetworkConnection conn, string defaultName)
        {
            if (conn == null) return;
            _playerCount.Value++;
            _playerStates[conn.ClientId] = new LobbyPlayerState(conn, defaultName);
        }

        [Server]
        public void RemovePlayer(int clientId)
        {
            _playerCount.Value--;
            if (_playerStates.ContainsKey(clientId))
                _playerStates.Remove(clientId);
        }

        [Server]
        public void SetPlayerName(int clientId, string name)
        {
            if (!_playerStates.TryGetValue(clientId, out LobbyPlayerState s))
                return;
            s.PlayerName = name;
            _playerStates[clientId] = s;
        }

        [Server]
        public void SetPlayerReady(int clientId, bool isReady)
        {
            if (!_playerStates.TryGetValue(clientId, out LobbyPlayerState s))
                return;
            s.IsReady = isReady;
            _playerStates[clientId] = s;
        }

        [Server]
        public void SetPlayerTeam(int clientId, int teamId)
        {
            if (!_playerStates.TryGetValue(clientId, out LobbyPlayerState s))
                return;
            // Keep the original NetworkConnection reference for this client
            s.TeamId = teamId;
            s.IsReady = false;
            _playerStates[clientId] = s;
        }

        public bool TryGetTeamInfo(int teamId, out LobbyTeamInfo info) => _teamInfos.TryGetValue(teamId, out info);

        [Server]
        public void AddTeamInfo(int teamId, LobbyTeamInfo info) => _teamInfos[teamId] = info;

        [Server]
        public void RemoveTeam(int teamId) => _teamInfos.Remove(teamId);

        public bool CheckAllPlayersReady()
        {
            foreach (KeyValuePair<int, LobbyPlayerState> kvp in _playerStates.Collection)
            {
                if (!kvp.Value.IsReady)
                    return false;
            }
            return _playerStates.Count > 0;
        }

        public bool ContainsPlayer(int clientId) => _playerStates.ContainsKey(clientId);

        public bool TryGetPlayersIDs(int? teamId, out int? driverConnectionId, out int? shotgunConnectionId)
        {
            driverConnectionId = null;
            shotgunConnectionId = null;
            if (teamId == null)
                return false;
            if (!_teamInfos.TryGetValue(teamId.Value, out LobbyTeamInfo teamInfo))
                return false;
            driverConnectionId = teamInfo.DriverConnectionId;
            shotgunConnectionId = teamInfo.ShotgunConnectionId;
            return true;
        }
    }
}
