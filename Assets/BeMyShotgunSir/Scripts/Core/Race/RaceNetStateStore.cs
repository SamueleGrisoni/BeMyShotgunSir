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

    // public struct RacePlayerInitData
    // {
    //     public bool IsHost;
    //     public int TeamId;
    //     public NetworkObject Player;
    //     public RaceRole Role;

    //     /// <summary>
    //     /// Create initial player init data.
    //     /// Player is intentionally set to <c>null</c> here because the NetworkObject
    //     /// instance for the player will be created/spawned later on the server and
    //     /// assigned in a second pass (see constructor overload that accepts a
    //     /// <see cref="NetworkObject"/>). Keeping the field null avoids holding
    //     /// invalid Unity object references during early state initialization.
    //     /// </summary>
    //     public RacePlayerInitData(bool isHost, NetworkObject player, int teamId, RaceRole role)
    //     {
    //         IsHost = isHost;
    //         // Intentionally not assigning `player` here; populated later when the
    //         // actual spawned NetworkObject is available.
    //         Player = null;
    //         TeamId = teamId;
    //         Role = role;
    //     }

    //     public RacePlayerInitData(RacePlayerInitData other, NetworkObject player)
    //     {
    //         IsHost = other.IsHost;
    //         Player = player;
    //         TeamId = other.TeamId;
    //         Role = other.Role;
    //     }

    //     public override string ToString() => $"IsHost: {IsHost}, Player: {(Player != null ? Player.name : "null")}, teamId: {TeamId}, Role: {Role}";
    // }

    public struct RacePlayerState
    {
        public int TeamId;
        public bool IsTrackReady;
        public bool IsReadyToRace;
        public RaceRole Role;

        public RacePlayerState(int teamId, bool isTrackReady, bool isReadyToRace, RaceRole role = RaceRole.None)
        {
            TeamId = teamId;
            IsTrackReady = isTrackReady;
            IsReadyToRace = isReadyToRace;
            Role = role;
        }

        public RacePlayerState(RacePlayerState other, bool? isTrackReady = null, bool? isReadyToRace = null, RaceRole? role = null)
        {
            TeamId = other.TeamId;
            IsTrackReady = isTrackReady ?? other.IsTrackReady;
            IsReadyToRace = isReadyToRace ?? other.IsReadyToRace;
            Role = role ?? other.Role;
        }

        public override string ToString() => $"TeamId: {TeamId}, IsTrackReady: {IsTrackReady}, IsReadyToRace: {IsReadyToRace}, Role: {Role}";
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
        public PowerUpType? PowerUp1;
        public PowerUpType? PowerUp2;
        public PowerUpType? PowerUp3;
        public PowerUpType? PowerUp4;
        public PowerUpType? PowerUp5;
        public int MaxPowerUps => 5;
        public PowerUpType? SelectedPowerUp;
        public bool IsFull => PowerUp1.HasValue && PowerUp2.HasValue && PowerUp3.HasValue && PowerUp4.HasValue && PowerUp5.HasValue;
        public bool IsEmpty => !PowerUp1.HasValue && !PowerUp2.HasValue && !PowerUp3.HasValue && !PowerUp4.HasValue && !PowerUp5.HasValue;

        public InventoryData(PowerUpType? powerUp1, PowerUpType? powerUp2, PowerUpType? powerUp3, PowerUpType? powerUp4, PowerUpType? powerUp5, PowerUpType? selectedPowerUp)
        {
            PowerUp1 = powerUp1;
            PowerUp2 = powerUp2;
            PowerUp3 = powerUp3;
            PowerUp4 = powerUp4;
            PowerUp5 = powerUp5;
            SelectedPowerUp = selectedPowerUp;
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

    // Compact accessor used by external components that need read-only state access.
    public interface IRaceNetState_Compact
    {
        IRaceNetStateRead NetState { get; }
    }

    public interface IRaceNetStateStore_Mutator : IRaceNetStateRead
    {
        void InitializeFromLobby(ILobbyNetStateRead lobbyState);
        void SetSeed(int value);
        void SetPlayerState(int connectionId, RacePlayerState playerState);
        void SetTeamData(int teamId, RaceTeamData teamData);
    }

    public interface IRaceNetStateStore_Projector
    {
        SyncDictionary<int, RacePlayerState> PlayerStatesSync { get; }
        SyncDictionary<int, RaceTeamData> TeamDataSync { get; }
        SyncList<int> LeaderboardSync { get; }
    }

    public interface IRaceNetStateStore : IRaceNetStateStore_Mutator, IRaceNetStateStore_Projector { }

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
        public SyncVar<int> Seed => _seed;
        public SyncDictionary<int, RacePlayerState> PlayerStatesSync => _racePlayerStates;
        public SyncDictionary<int, RaceTeamData> TeamDataSync => _raceTeamData;
        public SyncList<int> LeaderboardSync => _leaderboard;

        // State Read-only accessors
        public int GetSeed() => _seed.Value;
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _racePlayerStates;
        public IReadOnlyDictionary<int, RaceTeamData> TeamData => _raceTeamData;
        public IReadOnlyList<int> Leaderboard => _leaderboard;

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
                    teamId: lobbyPlayerState.Value.TeamId,
                    isTrackReady: false,
                    isReadyToRace: false
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
