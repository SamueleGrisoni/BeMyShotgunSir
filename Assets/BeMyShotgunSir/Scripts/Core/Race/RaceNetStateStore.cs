using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region DataStructures

    public enum RaceRole
    {
        None,
        Driver,
        Shotgun
    }

    public struct RacePlayerState
    {
        public string PlayerName;
        public int TeamId;
        public bool IsTrackReady;
        public bool IsReadyToRace;
        public RaceRole Role;

        public RacePlayerState(string name, int teamId, bool isTrackReady, bool isReadyToRace, RaceRole role = RaceRole.None)
        {
            PlayerName = name;
            TeamId = teamId;
            IsTrackReady = isTrackReady;
            IsReadyToRace = isReadyToRace;
            Role = role;
        }

        public RacePlayerState(RacePlayerState other, bool? isTrackReady = null, bool? isReadyToRace = null, RaceRole? role = null)
        {
            PlayerName = other.PlayerName;
            TeamId = other.TeamId;
            IsTrackReady = isTrackReady ?? other.IsTrackReady;
            IsReadyToRace = isReadyToRace ?? other.IsReadyToRace;
            Role = role ?? other.Role;
        }

        public override string ToString() => $"PlayerName: {PlayerName}, TeamId: {TeamId}, IsTrackReady: {IsTrackReady}, IsReadyToRace: {IsReadyToRace}, Role: {Role}";
    }

    public struct RaceTeamData
    {
        public int TeamId;
        public int DriverConnectionId;
        public int ShotgunConnectionId;
        public bool IsPlayerSpawned;
        public NetworkObject Player;
        public InventoryData? Inventory;

        public RaceTeamData(int teamId, int driverConnectionId, int shotgunConnectionId)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            ShotgunConnectionId = shotgunConnectionId;
            Inventory = null;
            Player = null;
            IsPlayerSpawned = false;
        }

        public RaceTeamData(RaceTeamData other, InventoryData? inventory = null, bool? isPlayerSpawned = null, NetworkObject player = null)
        {
            TeamId = other.TeamId;
            DriverConnectionId = other.DriverConnectionId;
            ShotgunConnectionId = other.ShotgunConnectionId;

            IsPlayerSpawned = isPlayerSpawned ?? other.IsPlayerSpawned;
            Player = player ?? other.Player;
            Inventory = inventory ?? other.Inventory;
        }

        public override string ToString() => $"TeamId: {TeamId}, DriverConnectionId: {DriverConnectionId}, ShotgunConnectionId: {ShotgunConnectionId}, Inventory: {Inventory}, IsPlayerSpawned: {IsPlayerSpawned}";
    }

    public struct InventoryData
    {
        public PowerUp? PowerUp1;
        public PowerUp? PowerUp2;
        public PowerUp? PowerUp3;
        public PowerUp? PowerUp4;
        public PowerUp? PowerUp5;
        public int MaxPowerUps => 5;
        public PowerUp? SelectedPowerUp;
        public PowerUp[] ActivePowerUps;
        public bool IsFull => PowerUp1.HasValue && PowerUp2.HasValue && PowerUp3.HasValue && PowerUp4.HasValue && PowerUp5.HasValue;
        public bool IsEmpty => !PowerUp1.HasValue && !PowerUp2.HasValue && !PowerUp3.HasValue && !PowerUp4.HasValue && !PowerUp5.HasValue;

        public InventoryData(PowerUp? powerUp1 = null, PowerUp? powerUp2 = null, PowerUp? powerUp3 = null, PowerUp? powerUp4 = null, PowerUp? powerUp5 = null, PowerUp? selectedPowerUp = null, PowerUp[] activePowerUps = null)
        {
            PowerUp1 = powerUp1;
            PowerUp2 = powerUp2;
            PowerUp3 = powerUp3;
            PowerUp4 = powerUp4;
            PowerUp5 = powerUp5;
            SelectedPowerUp = selectedPowerUp;
            ActivePowerUps = activePowerUps ?? new PowerUp[0];
        }

        public InventoryData(InventoryData other, PowerUp? powerUp1 = null, PowerUp? powerUp2 = null, PowerUp? powerUp3 = null, PowerUp? powerUp4 = null, PowerUp? powerUp5 = null, PowerUp? selectedPowerUp = null, PowerUp[] activePowerUps = null)
        {
            PowerUp1 = powerUp1 ?? other.PowerUp1;
            PowerUp2 = powerUp2 ?? other.PowerUp2;
            PowerUp3 = powerUp3 ?? other.PowerUp3;
            PowerUp4 = powerUp4 ?? other.PowerUp4;
            PowerUp5 = powerUp5 ?? other.PowerUp5;
            SelectedPowerUp = selectedPowerUp ?? other.SelectedPowerUp;
            ActivePowerUps = activePowerUps ?? other.ActivePowerUps;
        }

        public override string ToString() => $"PowerUp1: {PowerUp1}, PowerUp2: {PowerUp2}, PowerUp3: {PowerUp3}, PowerUp4: {PowerUp4}, PowerUp5: {PowerUp5}, SelectedPowerUp: {SelectedPowerUp}";
    }

    #endregion

    #region Interfaces

    public interface IRaceNetStateRead
    {
        int GetSeed();
        IReadOnlyDictionary<int, RacePlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, RaceTeamData> TeamData { get; }
        IReadOnlyList<int> Leaderboard { get; }
        bool TryGetPlayerState(int connectionId, out RacePlayerState playerState);
        bool TryGetTeamData(int teamId, out RaceTeamData teamData);
        bool AreAllPlayersReady();
    }

    public interface IRaceNetStateReadWrapped
    {
        IRaceNetStateRead NetState { get; }
    }

    public interface IRaceNetStateSubscribe : IRaceNetStateRead
    {
        SyncVar<int> Seed { get; }
        SyncDictionary<int, RacePlayerState> PlayerStatesSync { get; }
        SyncDictionary<int, RaceTeamData> TeamDataSync { get; }
        SyncList<int> LeaderboardSync { get; }
    }

    public interface IRaceNetStateStore : IRaceNetStateSubscribe { }

    #endregion


    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceClientProjector))]
    public sealed class RaceNetStateStore : NetworkBehaviour, IRaceNetStateStore
    {
        //utility
        private bool _log = true;
        public bool IsReady { get; private set; }
        public event Action OnReady;
        private void SetReady(bool value)
        {
            if (IsReady == value)
                return;
            IsReady = value;
            if (IsReady)
            {
                Log.DLazy(() => "RaceNetStateStore is ready.", this, _log);
                OnReady?.Invoke();
            }
        }

        // Networked state
        private readonly SyncVar<int> _seed = new(-1);
        private readonly SyncDictionary<int, RacePlayerState> _racePlayerStates = new();
        private readonly SyncDictionary<int, RaceTeamData> _raceTeamData = new();
        private readonly SyncList<int> _leaderboard = new();

        // State Projector accessors
        SyncVar<int> IRaceNetStateSubscribe.Seed => _seed;
        SyncDictionary<int, RacePlayerState> IRaceNetStateSubscribe.PlayerStatesSync => _racePlayerStates;
        SyncDictionary<int, RaceTeamData> IRaceNetStateSubscribe.TeamDataSync => _raceTeamData;
        SyncList<int> IRaceNetStateSubscribe.LeaderboardSync => _leaderboard;

        // State Read-only accessors
        int IRaceNetStateRead.GetSeed() => _seed.Value;
        IReadOnlyDictionary<int, RacePlayerState> IRaceNetStateRead.PlayerStates => _racePlayerStates;
        IReadOnlyDictionary<int, RaceTeamData> IRaceNetStateRead.TeamData => _raceTeamData;
        IReadOnlyList<int> IRaceNetStateRead.Leaderboard => _leaderboard;

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            SetReady(false);
        }

        [Server]
        public void InitializeFromLobby(ILobbyNetStateRead lobbyState)
        {
            _racePlayerStates.Collection.Clear();
            _raceTeamData.Collection.Clear();
            _leaderboard.Clear();
            _seed.Value = -1;

            foreach (KeyValuePair<int, LobbyPlayerState> lobbyPlayerState in lobbyState.PlayerStates)
            {
                _racePlayerStates[lobbyPlayerState.Key] = new RacePlayerState(
                    name: lobbyPlayerState.Value.PlayerName,
                    teamId: lobbyPlayerState.Value.TeamId,
                    isTrackReady: false,
                    isReadyToRace: false,
                    role: lobbyPlayerState.Value.ConnectionId == lobbyPlayerState.Value.TeamId ? RaceRole.Driver : RaceRole.Shotgun
                );
            }

            foreach (KeyValuePair<int, LobbyTeamInfo> teamInfo in lobbyState.TeamInfos)
            {
                _raceTeamData[teamInfo.Key] = new RaceTeamData(
                    teamId: teamInfo.Key,
                    driverConnectionId: teamInfo.Value.DriverConnectionId,
                    shotgunConnectionId: teamInfo.Value.ShotgunConnectionId
                );
            }

            SetReady(true);
        }
        [Server]
        public void SetSeed(int value) => _seed.Value = value;

        public bool TryGetPlayerState(int connectionId, out RacePlayerState playerState) =>
            _racePlayerStates.TryGetValue(connectionId, out playerState);

        [Server]
        public void SetPlayerState(int connectionId, RacePlayerState playerState) =>
            _racePlayerStates[connectionId] = playerState;

        public bool TryGetTeamData(int teamId, out RaceTeamData teamData) =>
            _raceTeamData.TryGetValue(teamId, out teamData);

        [Server]
        public void SetTeamData(int teamId, RaceTeamData teamData) =>
            _raceTeamData[teamId] = teamData;

        [Server]
        public void SetLeaderboard(List<int> orderedTeamIds)
        {
            _leaderboard.Clear();
            foreach (int teamId in orderedTeamIds)
                _leaderboard.Add(teamId);
        }

        public bool AreAllPlayersReady()
        {
            foreach (KeyValuePair<int, RacePlayerState> playerState in _racePlayerStates.Collection)
            {
                if (!playerState.Value.IsReadyToRace)
                    return false;
            }

            return _racePlayerStates.Count > 0;
        }
    }
}
