using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Utils;
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

    #region Interfaces


    public interface ILobbyNetStateRead
    {
        LobbyInfo LobbyInfo { get; }
        IReadOnlyDictionary<int, LobbyPlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, LobbyTeamInfo> TeamInfos { get; }
        int PlayerCount { get; }
        bool TryGetPlayerState(int clientId, out LobbyPlayerState state);
        bool TryGetTeamInfo(int teamId, out LobbyTeamInfo info);
        bool CheckAllPlayersReady();
        bool ContainsPlayer(int clientId);
    }

    public interface ILobbyNetStateReadWrapped
    {
        ILobbyNetStateRead NetState { get; }
    }

    public interface ILobbyNetStateSubscribe : ILobbyNetStateRead
    {
        SyncVar<LobbyInfo> LobbyInfoSync { get; }
        SyncDictionary<int, LobbyPlayerState> PlayerStatesSync { get; }
        SyncDictionary<int, LobbyTeamInfo> TeamInfosSync { get; }
        SyncVar<int> PlayerCountSync { get; }
    }

    public interface ILobbyNetStateStore : ILobbyNetStateSubscribe { }

    #endregion

    public sealed class LobbyNetStateStore : NetworkBehaviour, ILobbyNetStateStore
    {
        // utility
        private bool _log = true;
        public bool IsReady { get; private set; }
        public event Action OnReady;
        private void SetReady(bool ready)
        {
            if (ready == IsReady)
                return;

            IsReady = ready;
            if (IsReady)
            {
                Log.DLazy(() => "LobbyNetStateStore is ready.", this, _log);
                OnReady?.Invoke();
            }
        }

        //Networked state
        private readonly SyncVar<LobbyInfo> _lobbyInfo = new(default);
        private readonly SyncDictionary<int, LobbyPlayerState> _playerStates = new();
        private readonly SyncDictionary<int, LobbyTeamInfo> _teamInfos = new();
        private readonly SyncVar<int> _playerCount = new(0);

        //Subscriber accessors (Projectors)
        SyncVar<LobbyInfo> ILobbyNetStateSubscribe.LobbyInfoSync => _lobbyInfo;
        SyncDictionary<int, LobbyPlayerState> ILobbyNetStateSubscribe.PlayerStatesSync => _playerStates;
        SyncDictionary<int, LobbyTeamInfo> ILobbyNetStateSubscribe.TeamInfosSync => _teamInfos;
        SyncVar<int> ILobbyNetStateSubscribe.PlayerCountSync => _playerCount;

        //State Read-only accessors
        LobbyInfo ILobbyNetStateRead.LobbyInfo => _lobbyInfo.Value;
        IReadOnlyDictionary<int, LobbyPlayerState> ILobbyNetStateRead.PlayerStates => _playerStates;
        IReadOnlyDictionary<int, LobbyTeamInfo> ILobbyNetStateRead.TeamInfos => _teamInfos;
        int ILobbyNetStateRead.PlayerCount => _playerCount.Value;


        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            SetReady(false);
        }

        [Server]
        public void InitSyncValues()
        {
            _playerCount.Value = 0;
            string lobbyIP = GameServices.Instance.NetworkManager.TransportManager.Transport.GetServerBindAddress(FishNet.Transporting.IPAddressType.IPv4) + ":" + GameServices.Instance.NetworkManager.TransportManager.Transport.GetPort();
            _lobbyInfo.Value = new LobbyInfo(lobbyIP);
            _playerStates.Collection.Clear();
            _teamInfos.Collection.Clear();

            SetReady(true);
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
    }
}
