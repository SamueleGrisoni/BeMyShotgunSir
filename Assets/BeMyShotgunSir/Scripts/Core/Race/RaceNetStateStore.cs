using System.Collections.Generic;
using System.Linq;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
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
        public bool HasPlayerConnectionId(int connectionId) => DriverConnectionId == connectionId || ShotgunConnectionId == connectionId;

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
        public PowerUp SelectedSlot;
        public bool IsFull => Slot1.PowerUp != PowerUp.None && Slot2.PowerUp != PowerUp.None && Slot3.PowerUp != PowerUp.None && Slot4.PowerUp != PowerUp.None && Slot5.PowerUp != PowerUp.None;
        public bool IsEmpty => Slot1.PowerUp == PowerUp.None && Slot2.PowerUp == PowerUp.None && Slot3.PowerUp == PowerUp.None && Slot4.PowerUp == PowerUp.None && Slot5.PowerUp == PowerUp.None;

        public InventoryData(int teamId, PowerUp slot1 = PowerUp.None, PowerUp slot2 = PowerUp.None, PowerUp slot3 = PowerUp.None, PowerUp slot4 = PowerUp.None, PowerUp slot5 = PowerUp.None, PowerUp selectedSlot = PowerUp.None)
        {
            TeamId = teamId;
            Slot1 = slot1 != PowerUp.None ? new PuSlot(1, slot1) : new PuSlot(1, PowerUp.None);
            Slot2 = slot2 != PowerUp.None ? new PuSlot(2, slot2) : new PuSlot(2, PowerUp.None);
            Slot3 = slot3 != PowerUp.None ? new PuSlot(3, slot3) : new PuSlot(3, PowerUp.None);
            Slot4 = slot4 != PowerUp.None ? new PuSlot(4, slot4) : new PuSlot(4, PowerUp.None);
            Slot5 = slot5 != PowerUp.None ? new PuSlot(5, slot5) : new PuSlot(5, PowerUp.None);
            SelectedSlot = selectedSlot != PowerUp.None ? selectedSlot : PowerUp.None;
        }

        public InventoryData(InventoryData other, PowerUp slot1 = PowerUp.None, PowerUp slot2 = PowerUp.None, PowerUp slot3 = PowerUp.None, PowerUp slot4 = PowerUp.None, PowerUp slot5 = PowerUp.None, PowerUp selectedSlot = PowerUp.None)
        {
            TeamId = other.TeamId;
            Slot1 = slot1 != PowerUp.None ? new PuSlot(1, slot1) : other.Slot1;
            Slot2 = slot2 != PowerUp.None ? new PuSlot(2, slot2) : other.Slot2;
            Slot3 = slot3 != PowerUp.None ? new PuSlot(3, slot3) : other.Slot3;
            Slot4 = slot4 != PowerUp.None ? new PuSlot(4, slot4) : other.Slot4;
            Slot5 = slot5 != PowerUp.None ? new PuSlot(5, slot5) : other.Slot5;
            SelectedSlot = selectedSlot != PowerUp.None ? selectedSlot : other.SelectedSlot;
        }

        public PowerUp GetPowerUpInSlot(int slotIndex)
        {
            return slotIndex switch
            {
                1 => Slot1.PowerUp,
                2 => Slot2.PowerUp,
                3 => Slot3.PowerUp,
                4 => Slot4.PowerUp,
                5 => Slot5.PowerUp,
                _ => PowerUp.None
            };
        }

        public InventoryData SetFreeSlot(PowerUp powerUp, bool updateSelectedSlot = false)
        {
            if (IsEmpty)
                updateSelectedSlot = true;
            if (Slot1.PowerUp == PowerUp.None)
                return new InventoryData(this, slot1: powerUp, selectedSlot: updateSelectedSlot ? powerUp : SelectedSlot);
            if (Slot2.PowerUp == PowerUp.None)
                return new InventoryData(this, slot2: powerUp, selectedSlot: updateSelectedSlot ? powerUp : SelectedSlot);
            if (Slot3.PowerUp == PowerUp.None)
                return new InventoryData(this, slot3: powerUp, selectedSlot: updateSelectedSlot ? powerUp : SelectedSlot);
            if (Slot4.PowerUp == PowerUp.None)
                return new InventoryData(this, slot4: powerUp, selectedSlot: updateSelectedSlot ? powerUp : SelectedSlot);
            if (Slot5.PowerUp == PowerUp.None)
                return new InventoryData(this, slot5: powerUp, selectedSlot: updateSelectedSlot ? powerUp : SelectedSlot);

            Log.WLazy(() => $"Trying to add power-up {powerUp} to inventory but no free slots available.", this);
            return this;
        }

        public InventoryData RemovePowerUp(PowerUp powerUp)
        {
            if (Slot1.PowerUp == powerUp)
                return new InventoryData(this, slot1: PowerUp.None, selectedSlot: SelectedSlot == powerUp ? PowerUp.None : SelectedSlot);
            if (Slot2.PowerUp == powerUp)
                return new InventoryData(this, slot2: PowerUp.None, selectedSlot: SelectedSlot == powerUp ? PowerUp.None : SelectedSlot);
            if (Slot3.PowerUp == powerUp)
                return new InventoryData(this, slot3: PowerUp.None, selectedSlot: SelectedSlot == powerUp ? PowerUp.None : SelectedSlot);
            if (Slot4.PowerUp == powerUp)
                return new InventoryData(this, slot4: PowerUp.None, selectedSlot: SelectedSlot == powerUp ? PowerUp.None : SelectedSlot);
            if (Slot5.PowerUp == powerUp)
                return new InventoryData(this, slot5: PowerUp.None, selectedSlot: SelectedSlot == powerUp ? PowerUp.None : SelectedSlot);

            Log.WLazy(() => $"Trying to remove power-up {powerUp} from inventory but it was not found in any slot.", this);
            return this;
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

    public struct PortalInfo
    {
        public int Id;
        public RoadChunkType Position;
    }

    public struct TeamTrackProgress
    {
        public int CurrentChunkId;
        public int NextSpecialChunkId;
        public PortalInfo? LastSpecialChunkType;
        public bool IsFinishLineNext;

        public TeamTrackProgress(int nextForkId = 0, int nextJunctionId = 0, PortalInfo? lastSpecialPortalType = null, bool isFinishLineNext = false)
        {
            CurrentChunkId = nextForkId;
            NextSpecialChunkId = nextJunctionId;
            LastSpecialChunkType = lastSpecialPortalType;
            IsFinishLineNext = isFinishLineNext;
        }

        public TeamTrackProgress(TeamTrackProgress other, int? nextForkId = null, int? nextJunctionId = null, PortalInfo? lastSpecialPortalType = null, bool? isFinishLineNext = null)
        {
            CurrentChunkId = nextForkId ?? other.CurrentChunkId;
            NextSpecialChunkId = nextJunctionId ?? other.NextSpecialChunkId;
            LastSpecialChunkType = lastSpecialPortalType ?? other.LastSpecialChunkType;
            IsFinishLineNext = isFinishLineNext ?? other.IsFinishLineNext;
        }

        public override string ToString() => $"TeamTrackProgress: NextForkId: {CurrentChunkId}, NextJunctionId: {NextSpecialChunkId}, LastSpecialPortalType: {LastSpecialChunkType}, IsFinishLineNext: {IsFinishLineNext}";
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
        IReadOnlyDictionary<int, TeamTrackProgress> TeamTrackProgress { get; }
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
        SyncDictionary<int, TeamTrackProgress> TeamTrackProgress_Sub { get; }
    }

    public interface IRaceNetStateStore : IRaceNetStateSubscribe { }

    #endregion

    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceClientProjector))]
    public sealed class RaceNetStateStore : NetworkBehaviour, IRaceNetStateStore
    {

        # region Server-only state and methods
        //utility
        private bool _log = true;

        //Server-only states
        /// <summary> not synced </summary>
        private Dictionary<int, NetworkObject> _teamNobs = new();
        public IReadOnlyDictionary<int, NetworkObject> TeamNobs => _teamNobs;

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

        # endregion

        # region Networked state and methods
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
        /// <summary> synced </summary>
        private readonly SyncDictionary<int, TeamTrackProgress> _teamTrackProgress = new();

        // State Projector accessors
        SyncVar<int?> IRaceNetStateSubscribe.Seed_Sub => _seed;
        SyncDictionary<int, RacePlayerState> IRaceNetStateSubscribe.PlayerStates_Sub => _racePlayerStates;
        SyncDictionary<int, RaceTeamData> IRaceNetStateSubscribe.TeamData_Sub => _raceTeamData;
        SyncDictionary<int, InventoryData> IRaceNetStateSubscribe.PlayerInventories_Sub => _racePlayerInventories;
        SyncList<int> IRaceNetStateSubscribe.Leaderboard_Sub => _leaderboard;
        SyncDictionary<int, TeamTrackProgress> IRaceNetStateSubscribe.TeamTrackProgress_Sub => _teamTrackProgress;

        // State Read-only accessors
        public int? Seed => _seed.Value;
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _racePlayerStates;
        public IReadOnlyDictionary<int, RaceTeamData> TeamData => _raceTeamData;
        public IReadOnlyDictionary<int, InventoryData> PlayerInventories => _racePlayerInventories;
        public IReadOnlyList<int> Leaderboard => _leaderboard;
        public IReadOnlyDictionary<int, TeamTrackProgress> TeamTrackProgress => _teamTrackProgress;

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

        # endregion

        #region Setters

        // Seed
        [Server]
        public void SetSeed(int value) => _seed.Value = value;

        // Player state
        [Server]
        public void SetPlayerState(int connectionId, RacePlayerState playerState) =>
            _racePlayerStates[connectionId] = playerState;

        // Team data
        [Server]
        public void SetTeamData(int teamId, RaceTeamData teamData) => _raceTeamData[teamId] = teamData;

        // Inventory
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

        // Leaderboard
        [Server]
        public void SetLeaderboard(List<int> orderedTeamIds)
        {
            _leaderboard.Clear();
            foreach (int teamId in orderedTeamIds)
                _leaderboard.Add(teamId);
        }

        #endregion

        #region Getters

        // Player state
        public bool TryGetPlayerState(int connectionId, out RacePlayerState playerState) =>
            _racePlayerStates.TryGetValue(connectionId, out playerState);

        // Team data
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

        // Inventory
        public bool TryGetTeamInventory(int teamId, out InventoryData inventoryData) => _racePlayerInventories.TryGetValue(teamId, out inventoryData);

        public bool TryGetPlayerName(int connectionId, out string playerName)
        {
            playerName = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            playerName = playerState.PlayerName;
            return true;
        }

        public bool TryGetPlayerTeamId(int connectionId, out int teamId)
        {
            teamId = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            teamId = playerState.TeamId;
            return true;
        }

        public bool TryGetPlayerTrackReady(int connectionId, out bool isTrackReady)
        {
            isTrackReady = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            isTrackReady = playerState.IsTrackReady;
            return true;
        }

        public bool TryGetPlayerReadyToRace(int connectionId, out bool isReadyToRace)
        {
            isReadyToRace = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            isReadyToRace = playerState.IsReadyToRace;
            return true;
        }

        public bool TryGetPlayerRole(int connectionId, out RaceRole role)
        {
            role = default;
            if (!TryGetPlayerState(connectionId, out RacePlayerState playerState))
                return false;
            role = playerState.Role;
            return true;
        }

        public bool TryGetTeamDriverConnectionId(int teamId, out int driverConnectionId)
        {
            driverConnectionId = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            driverConnectionId = teamData.DriverConnectionId;
            return true;
        }

        public bool TryGetTeamShotgunConnectionId(int teamId, out int shotgunConnectionId)
        {
            shotgunConnectionId = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            shotgunConnectionId = teamData.ShotgunConnectionId;
            return true;
        }

        public bool TryGetTeamSpawned(int teamId, out bool isTeamSpawned)
        {
            isTeamSpawned = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            isTeamSpawned = teamData.IsTeamSpawned;
            return true;
        }

        public bool TryGetTeamDriverNob(int teamId, out NetworkObject driverNob)
        {
            driverNob = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            driverNob = teamData.DriverNob;
            return driverNob != null;
        }

        public bool TryGetTeamShotgunNob(int teamId, out NetworkObject shotgunNob)
        {
            shotgunNob = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            shotgunNob = teamData.ShotgunNob;
            return shotgunNob != null;
        }

        public bool TryGetTeamActivePowerUps(int teamId, out PowerUpIdentifier[] activePowerUps)
        {
            activePowerUps = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            activePowerUps = teamData.ActivePowerUps;
            return activePowerUps != null;
        }

        public bool TryGetTeamTargetedByPowerUps(int teamId, out PowerUpIdentifier[] targetedByPowerUps)
        {
            targetedByPowerUps = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            targetedByPowerUps = teamData.TargetedByPowerUps;
            return targetedByPowerUps != null;
        }

        public bool TryGetTeamActivePowerUpInfo(int teamId, out ActivePowerUpsInfo activePowerUpInfo)
        {
            activePowerUpInfo = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            activePowerUpInfo = teamData.ActivePowerUpInfo;
            return true;
        }

        public bool IsTeamTargetedByPowerUp(int teamId, PowerUp powerUp)
        {
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            return teamData.IsTargetedByPowerUp(powerUp);
        }

        public bool TeamHasActivePowerUp(int teamId, PowerUp powerUp)
        {
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            return teamData.HasActivePowerUp(powerUp);
        }

        public bool TeamHasPlayerConnectionId(int teamId, int connectionId)
        {
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            return teamData.HasPlayerConnectionId(connectionId);
        }

        public bool TryGetInventorySelectedSlot(int teamId, out PowerUp selectedSlot)
        {
            selectedSlot = default;
            if (!TryGetTeamInventory(teamId, out InventoryData inventoryData))
                return false;
            selectedSlot = inventoryData.SelectedSlot;
            return true;
        }

        public bool TryGetInventoryPowerUpInSlot(int teamId, int slotIndex, out PowerUp powerUp)
        {
            powerUp = default;
            if (!TryGetTeamInventory(teamId, out InventoryData inventoryData))
                return false;
            powerUp = inventoryData.GetPowerUpInSlot(slotIndex);
            return powerUp != PowerUp.None;
        }

        public bool TryGetInventoryIsFull(int teamId, out bool isFull)
        {
            isFull = default;
            if (!TryGetTeamInventory(teamId, out InventoryData inventoryData))
                return false;
            isFull = inventoryData.IsFull;
            return true;
        }

        public bool TryGetInventoryIsEmpty(int teamId, out bool isEmpty)
        {
            isEmpty = default;
            if (!TryGetTeamInventory(teamId, out InventoryData inventoryData))
                return false;
            isEmpty = inventoryData.IsEmpty;
            return true;
        }

        // Team track progress

        public bool TryGetTeamCurrentChunkId(int teamId, out int currentChunkId)
        {
            currentChunkId = default;
            if (!TeamTrackProgress.TryGetValue(teamId, out TeamTrackProgress trackProgress))
                return false;
            currentChunkId = trackProgress.CurrentChunkId;
            return true;
        }

        public bool TryGetTeamNextSpecialChunkId(int teamId, out int nextSpecialChunkId)
        {
            nextSpecialChunkId = default;
            if (!TeamTrackProgress.TryGetValue(teamId, out TeamTrackProgress trackProgress))
                return false;
            nextSpecialChunkId = trackProgress.NextSpecialChunkId;
            return true;
        }

        public bool TryGetTeamLastSpecialChunkType(int teamId, out PortalInfo? lastSpecialChunkType)
        {
            lastSpecialChunkType = default;
            if (!TeamTrackProgress.TryGetValue(teamId, out TeamTrackProgress trackProgress))
                return false;
            lastSpecialChunkType = trackProgress.LastSpecialChunkType;
            return true;
        }

        public bool TryGetTeamIsFinishLineNext(int teamId, out bool isFinishLineNext)
        {
            isFinishLineNext = default;
            if (!TeamTrackProgress.TryGetValue(teamId, out TeamTrackProgress trackProgress))
                return false;
            isFinishLineNext = trackProgress.IsFinishLineNext;
            return true;
        }

        public bool TryGetTeamTrackProgress(int teamId, out TeamTrackProgress trackProgress)
        {
            trackProgress = default;
            return TeamTrackProgress.TryGetValue(teamId, out trackProgress);
        }

        #endregion

        #region Utility methods

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

        public bool IsTeamMember(int localConnectionId, int teamId)
        {
            if (!TryGetTeamDataByPlayerId(localConnectionId, out RaceTeamData teamData))
            {
                Log.WLazy(() => $"No team data found for player {localConnectionId}.", this);
                return false;
            }
            return teamData.DriverConnectionId == localConnectionId || teamData.ShotgunConnectionId == localConnectionId;
        }

        public bool DidAllTeamsReachedChunk(int chunkId)
        {
            foreach (KeyValuePair<int, TeamTrackProgress> teamProgress in _teamTrackProgress.Collection)
            {
                if (teamProgress.Value.CurrentChunkId < chunkId)
                    return false;
            }
            return true;
        }

        public bool AreAllTeamsBeforeChunk(int chunkId)
        {
            foreach (KeyValuePair<int, TeamTrackProgress> teamProgress in _teamTrackProgress.Collection)
            {
                if (teamProgress.Value.CurrentChunkId >= chunkId)
                    return false;
            }
            return true;
        }

        #endregion
    }
}
