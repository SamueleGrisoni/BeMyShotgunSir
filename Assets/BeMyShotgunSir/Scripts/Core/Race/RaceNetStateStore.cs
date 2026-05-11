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
        public PowerUpIdentifier[] ActivePowerUps;
        public PowerUpIdentifier[] TargetedByPowerUps;
        public ActivePowerUpsInfo ActivePowerUpInfo;

        public RaceTeamData(int teamId, int driverConnectionId, int shotgunConnectionId)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            ShotgunConnectionId = shotgunConnectionId;
            TeamNob = null;
            DriverNob = null;
            ShotgunNob = null;
            IsTeamSpawned = false;
            ActivePowerUps = new PowerUpIdentifier[BMMSDefaults.MAX_ACTIVE_POWER_UPS];
            TargetedByPowerUps = new PowerUpIdentifier[BMMSDefaults.MAX_TARGETED_BY_POWER_UPS];
            ActivePowerUpInfo = default;
        }

        public RaceTeamData(RaceTeamData other, InventoryData? inventory = null, bool? isTeamSpawned = null, NetworkObject teamNob = null, NetworkObject driverNob = null, NetworkObject shotgunNob = null, PowerUpIdentifier[] activePowerUps = null, PowerUpIdentifier[] targetedByPowerUps = null, ActivePowerUpsInfo? activePowerUpInfo = null)
        {
            TeamId = other.TeamId;
            DriverConnectionId = other.DriverConnectionId;
            ShotgunConnectionId = other.ShotgunConnectionId;

            IsTeamSpawned = isTeamSpawned ?? other.IsTeamSpawned;
            TeamNob = teamNob ?? other.TeamNob;
            DriverNob = driverNob ?? other.DriverNob;
            ShotgunNob = shotgunNob ?? other.ShotgunNob;
            ActivePowerUps = activePowerUps ?? other.ActivePowerUps;
            TargetedByPowerUps = targetedByPowerUps ?? other.TargetedByPowerUps;
            ActivePowerUpInfo = activePowerUpInfo ?? other.ActivePowerUpInfo;
        }

        public override string ToString() => $"TeamId: {TeamId}, DriverConnectionId: {DriverConnectionId}, ShotgunConnectionId: {ShotgunConnectionId}, IsTeamSpawned: {IsTeamSpawned}, TeamNob: {TeamNob}, DriverNob: {DriverNob}, ShotgunNob: {ShotgunNob}, ActivePowerUps: [{string.Join(", ", ActivePowerUps)}], TargetedByPowerUps: [{string.Join(", ", TargetedByPowerUps)}], ActivePowerUpInfo: {ActivePowerUpInfo}";

        public bool IsTargetedByPowerUp(PowerUp powerUp) => TargetedByPowerUps.Any(identifier => identifier.PowerUp == powerUp);
        public bool HasActivePowerUp(PowerUp powerUp) => ActivePowerUps.Any(identifier => identifier.PowerUp == powerUp);

    }

    public struct PuSlot
    {
        public int SlotIndex;
        public PowerUp PowerUp;

        public PuSlot(int slotIndex, PowerUp powerUp)
        {
            SlotIndex = slotIndex;
            PowerUp = powerUp;
        }
    }

    public struct InventoryData
    {
        public int TeamId;
        public PuSlot Slot1;
        public PuSlot Slot2;
        public PuSlot Slot3;
        public PuSlot Slot4;
        public PuSlot Slot5;
        public int MaxPowerUps => 5;
        public PowerUp? SelectedSlot;
        public bool IsFull => Slot1.PowerUp != PowerUp.None && Slot2.PowerUp != PowerUp.None && Slot3.PowerUp != PowerUp.None && Slot4.PowerUp != PowerUp.None && Slot5.PowerUp != PowerUp.None;
        public bool IsEmpty => Slot1.PowerUp == PowerUp.None && Slot2.PowerUp == PowerUp.None && Slot3.PowerUp == PowerUp.None && Slot4.PowerUp == PowerUp.None && Slot5.PowerUp == PowerUp.None;

        public InventoryData(int teamId, PowerUp slot1 = PowerUp.None, PowerUp slot2 = PowerUp.None, PowerUp slot3 = PowerUp.None, PowerUp slot4 = PowerUp.None, PowerUp slot5 = PowerUp.None, PowerUp? selectedSlot = null)
        {
            TeamId = teamId;
            Slot1 = slot1 != PowerUp.None ? new PuSlot(1, slot1) : new PuSlot(1, PowerUp.None);
            Slot2 = slot2 != PowerUp.None ? new PuSlot(2, slot2) : new PuSlot(2, PowerUp.None);
            Slot3 = slot3 != PowerUp.None ? new PuSlot(3, slot3) : new PuSlot(3, PowerUp.None);
            Slot4 = slot4 != PowerUp.None ? new PuSlot(4, slot4) : new PuSlot(4, PowerUp.None);
            Slot5 = slot5 != PowerUp.None ? new PuSlot(5, slot5) : new PuSlot(5, PowerUp.None);
            SelectedSlot = selectedSlot;
        }

        public InventoryData(InventoryData other, PowerUp slot1 = PowerUp.None, PowerUp slot2 = PowerUp.None, PowerUp slot3 = PowerUp.None, PowerUp slot4 = PowerUp.None, PowerUp slot5 = PowerUp.None, PowerUp? selectedSlot = null)
        {
            TeamId = other.TeamId;
            Slot1 = slot1 != PowerUp.None ? new PuSlot(1, slot1) : other.Slot1;
            Slot2 = slot2 != PowerUp.None ? new PuSlot(2, slot2) : other.Slot2;
            Slot3 = slot3 != PowerUp.None ? new PuSlot(3, slot3) : other.Slot3;
            Slot4 = slot4 != PowerUp.None ? new PuSlot(4, slot4) : other.Slot4;
            Slot5 = slot5 != PowerUp.None ? new PuSlot(5, slot5) : other.Slot5;
            SelectedSlot = selectedSlot != null ? selectedSlot : other.SelectedSlot;
        }

        public override string ToString() => $"Slot1: {Slot1.PowerUp}, Slot2: {Slot2.PowerUp}, Slot3: {Slot3.PowerUp}, Slot4: {Slot4.PowerUp}, Slot5: {Slot5.PowerUp}, SelectedSlot: {SelectedSlot}";
    }

    public struct ActivePowerUpsInfo
    {
        public bool isShieldActive;
        public bool isArmorActive;
        public bool isInvisibilityActive;
        public bool isStealPowerUpActive;
        public bool isRerollPowerUpActive;
        public bool isRoadBlockActive;
        public override string ToString() => $"ActivePowerUpInfo: isShieldActive: {isShieldActive}, isArmorActive: {isArmorActive}, isInvisibilityActive: {isInvisibilityActive}, isStealPowerUpActive: {isStealPowerUpActive}, isRerollPowerUpActive: {isRerollPowerUpActive}, isRoadBlockActive: {isRoadBlockActive}";
        public ActivePowerUpsInfo(bool isShieldActive = false, bool isArmorActive = false, bool isInvisibilityActive = false, bool isStealPowerUpActive = false, bool isRerollPowerUpActive = false, bool isRoadBlockActive = false)
        {
            this.isShieldActive = isShieldActive;
            this.isArmorActive = isArmorActive;
            this.isInvisibilityActive = isInvisibilityActive;
            this.isStealPowerUpActive = isStealPowerUpActive;
            this.isRerollPowerUpActive = isRerollPowerUpActive;
            this.isRoadBlockActive = isRoadBlockActive;
        }
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
        bool TryGetTeamInventory(int teamId, out InventoryData inventoryData);
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

        public bool TryGetTeamNob(int teamId, out NetworkObject nob) =>
            _teamNobs.TryGetValue(teamId, out nob);

        public bool TryGetTeamNobByPlayerId(int connectionId, out NetworkObject nob)
        {
            nob = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            return TryGetTeamNob(playerState.TeamId, out nob);
        }

        public bool TryGetDriverNob(int teamId, out NetworkObject nob)
        {
            nob = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            nob = teamData.DriverNob;
            return nob != null;
        }

        public bool TryGetShotgunNob(int teamId, out NetworkObject nob)
        {
            nob = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            nob = teamData.ShotgunNob;
            return nob != null;
        }
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

            foreach (KeyValuePair<int, RaceTeamData> teamData in _raceTeamData)
            {
                _racePlayerInventories[teamData.Key] = new InventoryData(teamId: teamData.Key);
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

        public bool TryGetTeamDataByPlayerId(int connectionId, out RaceTeamData teamData)
        {
            teamData = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            return TryGetTeamData(playerState.TeamId, out teamData);
        }

        public bool TryGetTeamDataByPlayer(RacePlayerState playerState, out RaceTeamData teamData) => TryGetTeamData(playerState.TeamId, out teamData);

        [Server]
        public void SetTeamData(int teamId, RaceTeamData teamData) => _raceTeamData[teamId] = teamData;

        [Server]
        public void SetPlayerInventory(int teamId, InventoryData inventoryData) => _racePlayerInventories[teamId] = inventoryData;

        [Server]
        public void SetSelectedPowerUp(int teamId, int slot)
        {
            if (!_racePlayerInventories.TryGetValue(teamId, out InventoryData inventoryData))
            {
                Log.WLazy(() => $"Trying to set selected power-up for team {teamId} but no inventory data found for this team.", this);
                return;
            }
            switch (slot)
            {
                case 1:
                    inventoryData = new InventoryData(inventoryData, slot1: inventoryData.Slot1.PowerUp);
                    break;
                case 2:
                    inventoryData = new InventoryData(inventoryData, slot2: inventoryData.Slot2.PowerUp);
                    break;
                case 3:
                    inventoryData = new InventoryData(inventoryData, slot3: inventoryData.Slot3.PowerUp);
                    break;
                case 4:
                    inventoryData = new InventoryData(inventoryData, slot4: inventoryData.Slot4.PowerUp);
                    break;
                case 5:
                    inventoryData = new InventoryData(inventoryData, slot5: inventoryData.Slot5.PowerUp);
                    break;
                default:
                    Log.WLazy(() => $"Trying to set selected power-up for team {teamId} but invalid slot index {slot}.", this);
                    return;
            }
            SetPlayerInventory(teamId, inventoryData);
        }

        public bool TryGetTeamInventory(int teamId, out InventoryData inventoryData) => _racePlayerInventories.TryGetValue(teamId, out inventoryData);

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

        public bool TryUnwrapConnectionState(int connectionId, out RacePlayerState playerState, out RaceTeamData teamData, out InventoryData inventory)
        {
            playerState = default;
            teamData = default;
            inventory = default;

            if (!TryGetPlayerState(connectionId, out RacePlayerState p))
            {
                Log.WLazy(() => $"No player state found for ConnectionId: {connectionId}.", this);
                return false;
            }
            if (!TryGetTeamDataByPlayer(playerState, out RaceTeamData t))
            {
                Log.WLazy(() => $"No team data found for player {p.TeamId}.", this);
                return false;
            }
            if (!TryGetTeamInventory(playerState.TeamId, out InventoryData i))
            {
                Log.WLazy(() => $"No inventory found for team {p.TeamId}.", this);
                return false;
            }
            playerState = p;
            teamData = t;
            inventory = i;
            return true;
        }
    }
}
