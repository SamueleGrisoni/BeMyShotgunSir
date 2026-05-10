using System.Collections.Generic;
using System.Linq;
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
        public bool IsTeamSpawned;
        public NetworkObject TeamNob;
        public NetworkObject DriverNob;
        public NetworkObject ShotgunNob;

        public RaceTeamData(int teamId, int driverConnectionId, int shotgunConnectionId)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            ShotgunConnectionId = shotgunConnectionId;
            TeamNob = null;
            DriverNob = null;
            ShotgunNob = null;
            IsTeamSpawned = false;
        }

        public RaceTeamData(RaceTeamData other, InventoryData? inventory = null, bool? isTeamSpawned = null, NetworkObject teamNob = null, NetworkObject driverNob = null, NetworkObject shotgunNob = null)
        {
            TeamId = other.TeamId;
            DriverConnectionId = other.DriverConnectionId;
            ShotgunConnectionId = other.ShotgunConnectionId;

            IsTeamSpawned = isTeamSpawned ?? other.IsTeamSpawned;
            TeamNob = teamNob ?? other.TeamNob;
            DriverNob = driverNob ?? other.DriverNob;
            ShotgunNob = shotgunNob ?? other.ShotgunNob;
        }

        public override string ToString() => $"TeamId: {TeamId}, DriverConnectionId: {DriverConnectionId}, ShotgunConnectionId: {ShotgunConnectionId}, IsTeamSpawned: {IsTeamSpawned}";
    }

    public struct InventoryData
    {
        public PowerUp? Slot1;
        public PowerUp? Slot2;
        public PowerUp? Slot3;
        public PowerUp? Slot4;
        public PowerUp? Slot5;
        public int MaxPowerUps => 5;
        public PowerUp? SelectedSlot;
        public PowerUp[] ActivePowerUps;
        public bool IsFull => Slot1.HasValue && Slot2.HasValue && Slot3.HasValue && Slot4.HasValue && Slot5.HasValue;
        public bool IsEmpty => !Slot1.HasValue && !Slot2.HasValue && !Slot3.HasValue && !Slot4.HasValue && !Slot5.HasValue;

        public InventoryData(PowerUp? slot1 = null, PowerUp? slot2 = null, PowerUp? slot3 = null, PowerUp? slot4 = null, PowerUp? slot5 = null, PowerUp? selectedSlot = null, PowerUp[] activePowerUps = null)
        {
            Slot1 = slot1;
            Slot2 = slot2;
            Slot3 = slot3;
            Slot4 = slot4;
            Slot5 = slot5;
            SelectedSlot = selectedSlot;
            ActivePowerUps = activePowerUps ?? new PowerUp[0];
        }

        public InventoryData(InventoryData other, PowerUp? slot1 = null, PowerUp? slot2 = null, PowerUp? slot3 = null, PowerUp? slot4 = null, PowerUp? slot5 = null, PowerUp? selectedSlot = null, PowerUp[] activePowerUps = null)
        {
            Slot1 = slot1 ?? other.Slot1;
            Slot2 = slot2 ?? other.Slot2;
            Slot3 = slot3 ?? other.Slot3;
            Slot4 = slot4 ?? other.Slot4;
            Slot5 = slot5 ?? other.Slot5;
            SelectedSlot = selectedSlot ?? other.SelectedSlot;
            ActivePowerUps = activePowerUps ?? other.ActivePowerUps;
        }

        public override string ToString() => $"Slot1: {Slot1}, Slot2: {Slot2}, Slot3: {Slot3}, Slot4: {Slot4}, Slot5: {Slot5}, SelectedSlot: {SelectedSlot}";
    }

    #endregion

    #region Interfaces

    public interface IRaceNetStateRead
    {
        int? Seed { get; }
        IReadOnlyDictionary<int, RacePlayerState> PlayerStates { get; }
        IReadOnlyDictionary<int, RaceTeamData> TeamData { get; }
        IReadOnlyDictionary<int, InventoryData> PlayerInventories { get; }
        IReadOnlyList<int> Leaderboard { get; }
        bool TryGetPlayerState(int connectionId, out RacePlayerState playerState);
        bool TryGetTeamData(int teamId, out RaceTeamData teamData);
        bool TryGetPlayerInventory(int teamId, out InventoryData inventoryData);
        bool AreAllPlayersReady();
    }

    public interface IRaceNetStateSubscribe : IRaceNetStateRead
    {
        SyncVar<int?> Seed_Sub { get; }
        SyncDictionary<int, RacePlayerState> PlayerStates_Sub { get; }
        SyncDictionary<int, RaceTeamData> TeamData_Sub { get; }
        SyncList<int> Leaderboard_Sub { get; }
        SyncDictionary<int, InventoryData> PlayerInventories_Sub { get; }
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

        //Server-only states
        /// <summary> not synced </summary>
        private Dictionary<int, NetworkObject> _teamNobs = new();
        public IReadOnlyDictionary<int, NetworkObject> TeamNobs => _teamNobs;

        [Server]
        public void RegisterTeamNob(int teamId, NetworkObject nob)
        {
            if (_teamNobs.ContainsKey(teamId))
                Log.WLazy(() => $"Team {teamId} already has a registered nob. Overwriting with new nob.", this);
            _teamNobs[teamId] = nob;
        }

        [Server]
        public bool TryGetTeamNob(int teamId, out NetworkObject nob) =>
            _teamNobs.TryGetValue(teamId, out nob);

        // Networked state
        /// <summary> synced </summary>
        private readonly SyncVar<int?> _seed = new();
        /// <summary> synced </summary>
        private readonly SyncDictionary<int, RacePlayerState> _racePlayerStates = new();
        /// <summary> synced </summary>
        private readonly SyncDictionary<int, RaceTeamData> _raceTeamData = new();
        /// <summary> synced </summary>
        private readonly SyncDictionary<int, InventoryData> _racePlayerInventories = new();
        /// <summary> synced </summary>
        private readonly SyncList<int> _leaderboard = new();

        // State Projector accessors
        SyncVar<int?> IRaceNetStateSubscribe.Seed_Sub => _seed;
        SyncDictionary<int, RacePlayerState> IRaceNetStateSubscribe.PlayerStates_Sub => _racePlayerStates;
        SyncDictionary<int, RaceTeamData> IRaceNetStateSubscribe.TeamData_Sub => _raceTeamData;
        SyncDictionary<int, InventoryData> IRaceNetStateSubscribe.PlayerInventories_Sub => _racePlayerInventories;
        SyncList<int> IRaceNetStateSubscribe.Leaderboard_Sub => _leaderboard;

        // State Read-only accessors
        public int? Seed => _seed.Value;
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _racePlayerStates;
        public IReadOnlyDictionary<int, RaceTeamData> TeamData => _raceTeamData;
        public IReadOnlyDictionary<int, InventoryData> PlayerInventories => _racePlayerInventories;
        public IReadOnlyList<int> Leaderboard => _leaderboard;

        public void PrintRaceState(bool log = true)
        {
            if (!log) return;
            Log.DLazy(() =>
            {
                string playerStatesStr = string.Join(", ", PlayerStates.Select(kvp => $"[ConnectionId: {kvp.Key}, State: {kvp.Value}]"));
                string teamDataStr = string.Join(", ", TeamData.Select(kvp => $"[TeamId: {kvp.Key}, Data: {kvp.Value}]"));
                string inventoryStr = string.Join(", ", PlayerInventories.Select(kvp => $"[TeamId: {kvp.Key}, Inventory: {kvp.Value}]"));
                return $"Race State: Seed: {Seed}, PlayerStates: {playerStatesStr}, TeamData: {teamDataStr}, PlayerInventories: {inventoryStr}, Leaderboard: [{string.Join(", ", Leaderboard)}]";
            }, this, _log);
        }


        [Server]
        public void InitializeFromLobby(ILobbyNetStateRead lobbyState)
        {
            _racePlayerStates.Collection.Clear();
            _raceTeamData.Collection.Clear();
            _leaderboard.Clear();

            foreach (KeyValuePair<int, LobbyPlayerState> lobbyPlayerState in lobbyState.PlayerStates)
            {
                if (lobbyPlayerState.Value.TeamId is not int)
                {
                    Log.ELazy(() => $"Player {lobbyPlayerState.Value.PlayerName} (ConnectionId: {lobbyPlayerState.Key}) does not have a team assigned in the lobby.", this);
                    return;
                }
            }

            foreach (KeyValuePair<int, LobbyPlayerState> lobbyPlayerState in lobbyState.PlayerStates)
            {
                int teamId = lobbyPlayerState.Value.TeamId.Value;

                _racePlayerStates[lobbyPlayerState.Key] = new RacePlayerState(
                    name: lobbyPlayerState.Value.PlayerName,
                    teamId: teamId,
                    isTrackReady: false,
                    isReadyToRace: false,
                    role: lobbyPlayerState.Value.ConnectionId == teamId ? RaceRole.Driver : RaceRole.Shotgun
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
        public void SetPlayerInventory(int teamId, InventoryData inventoryData) =>
            _racePlayerInventories[teamId] = inventoryData;

        public bool TryGetPlayerInventory(int teamId, out InventoryData inventoryData) =>
            _racePlayerInventories.TryGetValue(teamId, out inventoryData);

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
            PrintRaceState(_log);
            return _racePlayerStates.Count > 0;
        }
    }
}
