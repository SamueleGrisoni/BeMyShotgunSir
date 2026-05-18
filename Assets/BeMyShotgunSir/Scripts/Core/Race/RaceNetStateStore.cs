using System;
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
            ActivePowerUps = new PowerUpIdentifier[1];
            TargetedByPowerUps = new PowerUpIdentifier[1];
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

        public RaceTeamData RemoveActivePowerUpResult(int instanceId)
        {
            PowerUpIdentifier[] newActivePowerUps = ActivePowerUps.Where(identifier => identifier.InstanceId != instanceId).ToArray();
            return new RaceTeamData(this, activePowerUps: newActivePowerUps);
        }

        public RaceTeamData AddActivePowerUpResult(PowerUpIdentifier identifier)
        {
            PowerUpIdentifier[] newActivePowerUps = ActivePowerUps.Append(identifier).ToArray();
            return new RaceTeamData(this, activePowerUps: newActivePowerUps);
        }

        public RaceTeamData RemoveTargetedByPowerUpResult(int instanceId)
        {
            PowerUpIdentifier[] newTargetedByPowerUps = TargetedByPowerUps.Where(identifier => identifier.InstanceId != instanceId).ToArray();
            return new RaceTeamData(this, targetedByPowerUps: newTargetedByPowerUps);
        }

        public RaceTeamData AddTargetedByPowerUpResult(PowerUpIdentifier identifier, int targetTeamId)
        {
            if (targetTeamId != TeamId)
                return this;
            PowerUpIdentifier[] newTargetedByPowerUps = TargetedByPowerUps.Append(identifier).ToArray();
            return new RaceTeamData(this, targetedByPowerUps: newTargetedByPowerUps);
        }

        public override string ToString() => $"TeamId: {TeamId}, \nDriverConnectionId: {DriverConnectionId}, \nShotgunConnectionId: {ShotgunConnectionId}, \nIsTeamSpawned: {IsTeamSpawned}, \nTeamNob: {TeamNob}, \nDriverNob: {DriverNob}, \nShotgunNob: {ShotgunNob}, \nActivePowerUps: [{string.Join(",\n ", ActivePowerUps)}], \nTargetedByPowerUps: [{string.Join(",\n ", TargetedByPowerUps)}], \nActivePowerUpInfo: {ActivePowerUpInfo}";

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

        public override string ToString() => $"SlotIndex: {SlotIndex}, PowerUp: {PowerUp}";
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
        public PuSlot? SelectedSlot;
        public bool IsFull => Slot1.PowerUp != PowerUp.None && Slot2.PowerUp != PowerUp.None && Slot3.PowerUp != PowerUp.None && Slot4.PowerUp != PowerUp.None && Slot5.PowerUp != PowerUp.None;
        public bool IsEmpty => Slot1.PowerUp == PowerUp.None && Slot2.PowerUp == PowerUp.None && Slot3.PowerUp == PowerUp.None && Slot4.PowerUp == PowerUp.None && Slot5.PowerUp == PowerUp.None;

        public InventoryData(int teamId, PowerUp slot1 = PowerUp.None, PowerUp slot2 = PowerUp.None, PowerUp slot3 = PowerUp.None, PowerUp slot4 = PowerUp.None, PowerUp slot5 = PowerUp.None, PuSlot? selectedSlot = null)
        {
            TeamId = teamId;
            Slot1 = slot1 != PowerUp.None ? new PuSlot(1, slot1) : new PuSlot(1, PowerUp.None);
            Slot2 = slot2 != PowerUp.None ? new PuSlot(2, slot2) : new PuSlot(2, PowerUp.None);
            Slot3 = slot3 != PowerUp.None ? new PuSlot(3, slot3) : new PuSlot(3, PowerUp.None);
            Slot4 = slot4 != PowerUp.None ? new PuSlot(4, slot4) : new PuSlot(4, PowerUp.None);
            Slot5 = slot5 != PowerUp.None ? new PuSlot(5, slot5) : new PuSlot(5, PowerUp.None);
            SelectedSlot = selectedSlot;
        }

        public InventoryData(InventoryData other, PuSlot? slot1 = null, PuSlot? slot2 = null, PuSlot? slot3 = null, PuSlot? slot4 = null, PuSlot? slot5 = null, PuSlot? selectedSlot = null)
        {
            TeamId = other.TeamId;
            Slot1 = slot1 ?? other.Slot1;
            Slot2 = slot2 ?? other.Slot2;
            Slot3 = slot3 ?? other.Slot3;
            Slot4 = slot4 ?? other.Slot4;
            Slot5 = slot5 ?? other.Slot5;
            SelectedSlot = selectedSlot ?? other.SelectedSlot;
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
                return new InventoryData(this, slot1: new PuSlot(1, powerUp), selectedSlot: updateSelectedSlot ? new PuSlot(1, powerUp) : SelectedSlot);
            if (Slot2.PowerUp == PowerUp.None)
                return new InventoryData(this, slot2: new PuSlot(2, powerUp), selectedSlot: updateSelectedSlot ? new PuSlot(2, powerUp) : SelectedSlot);
            if (Slot3.PowerUp == PowerUp.None)
                return new InventoryData(this, slot3: new PuSlot(3, powerUp), selectedSlot: updateSelectedSlot ? new PuSlot(3, powerUp) : SelectedSlot);
            if (Slot4.PowerUp == PowerUp.None)
                return new InventoryData(this, slot4: new PuSlot(4, powerUp), selectedSlot: updateSelectedSlot ? new PuSlot(4, powerUp) : SelectedSlot);
            if (Slot5.PowerUp == PowerUp.None)
                return new InventoryData(this, slot5: new PuSlot(5, powerUp), selectedSlot: updateSelectedSlot ? new PuSlot(5, powerUp) : SelectedSlot);

            Log.WLazy(() => $"Trying to add power-up {powerUp} to inventory but no free slots available.", this);
            return this;
        }

        public InventoryData RemoveAndUpdateSelected()
        {
            if (SelectedSlot == null)
            {
                Log.WLazy(() => $"Trying to remove power-up but it was already null.", this);
                return this;
            }
            InventoryData updatedInventory = SelectedSlot.Value.SlotIndex switch
            {
                1 => new InventoryData(this, slot1: new PuSlot(1, PowerUp.None), selectedSlot: SelectedSlot.Value),
                2 => new InventoryData(this, slot2: new PuSlot(2, PowerUp.None), selectedSlot: SelectedSlot.Value),
                3 => new InventoryData(this, slot3: new PuSlot(3, PowerUp.None), selectedSlot: SelectedSlot.Value),
                4 => new InventoryData(this, slot4: new PuSlot(4, PowerUp.None), selectedSlot: SelectedSlot.Value),
                5 => new InventoryData(this, slot5: new PuSlot(5, PowerUp.None), selectedSlot: SelectedSlot.Value),
                _ => this
            };

            if (updatedInventory.Equals(this))
            {
                Log.WLazy(() => $"Trying to remove power-up but selected slot index is invalid.", this);
                return this;
            }

            return updatedInventory.UpdateSelectedSlot(SelectedSlotPolicy.CLosestNonEmptyIndex);
        }

        public InventoryData UpdateSelectedSlot(SelectedSlotPolicy policy)
        {
            return policy switch
            {
                SelectedSlotPolicy.LowerNonEmptyIndex => UpdateSelectedSlot(-1),
                SelectedSlotPolicy.CLosestNonEmptyIndex => UpdateSelectedSlot(-2),
                _ => this
            };
        }

        /// <summary>
        /// Updates the selected slot based on the given slot index. If slotIndex is -1, it selects the first non-empty slot.
        /// If slotIndex is -2, it tries to keep the current selected slot if it's not empty,
        /// otherwise it selects the closest non-empty slot. For any other positive slotIndex,
        ///  it selects that specific slot if it's not empty. Returns the updated InventoryData with the new selected slot.
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public InventoryData UpdateSelectedSlot(int slotIndex = -1)
        {
            if (slotIndex == -1)
            {
                if (Slot1.PowerUp != PowerUp.None)
                    slotIndex = 1;
                else if (Slot2.PowerUp != PowerUp.None)
                    slotIndex = 2;
                else if (Slot3.PowerUp != PowerUp.None)
                    slotIndex = 3;
                else if (Slot4.PowerUp != PowerUp.None)
                    slotIndex = 4;
                else if (Slot5.PowerUp != PowerUp.None)
                    slotIndex = 5;
                else
                    return new InventoryData(this, selectedSlot: null);
            }
            else if (slotIndex == -2)
            {
                if (!SelectedSlot.HasValue)
                    return new InventoryData(this, selectedSlot: null);

                int currentIndex = SelectedSlot.Value.SlotIndex;
                if (GetPowerUpInSlot(currentIndex) != PowerUp.None)
                    slotIndex = currentIndex;
                else
                {
                    bool found = false;
                    for (int offset = 1; offset <= MaxPowerUps && !found; offset++)
                    {
                        int leftIndex = currentIndex - offset;
                        if (leftIndex >= 1 && GetPowerUpInSlot(leftIndex) != PowerUp.None)
                        {
                            slotIndex = leftIndex;
                            found = true;
                            break;
                        }

                        int rightIndex = currentIndex + offset;
                        if (rightIndex <= MaxPowerUps && GetPowerUpInSlot(rightIndex) != PowerUp.None)
                        {
                            slotIndex = rightIndex;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        return new InventoryData(this, selectedSlot: null);
                }
            }
            return slotIndex switch
            {
                1 when Slot1.PowerUp != PowerUp.None => new InventoryData(this, selectedSlot: new PuSlot(1, Slot1.PowerUp)),
                2 when Slot2.PowerUp != PowerUp.None => new InventoryData(this, selectedSlot: new PuSlot(2, Slot2.PowerUp)),
                3 when Slot3.PowerUp != PowerUp.None => new InventoryData(this, selectedSlot: new PuSlot(3, Slot3.PowerUp)),
                4 when Slot4.PowerUp != PowerUp.None => new InventoryData(this, selectedSlot: new PuSlot(4, Slot4.PowerUp)),
                5 when Slot5.PowerUp != PowerUp.None => new InventoryData(this, selectedSlot: new PuSlot(5, Slot5.PowerUp)),
                _ => new InventoryData(this, selectedSlot: null)
            };
        }

        public override string ToString() => $"Slot1: {Slot1.PowerUp}, Slot2: {Slot2.PowerUp}, Slot3: {Slot3.PowerUp}, Slot4: {Slot4.PowerUp}, Slot5: {Slot5.PowerUp}, SelectedSlot: {SelectedSlot}";
    }

    public struct ActivePowerUpsInfo
    {
        public bool isShieldActive;
        public bool isArmorActive;
        public bool isInvisibilityActive;
        public bool isSpearPowerUpActive;
        public bool isStealPowerUpActive;
        public bool isRerollPowerUpActive;
        public bool isRoadBlockActive;
        public bool isTargetedBySpear;

        public override string ToString() => $"ActivePowerUpInfo: isShieldActive: {isShieldActive}, isArmorActive: {isArmorActive}, isInvisibilityActive: {isInvisibilityActive}, isSpearPowerUpActive: {isSpearPowerUpActive}, isStealPowerUpActive: {isStealPowerUpActive}, isRerollPowerUpActive: {isRerollPowerUpActive}, isRoadBlockActive: {isRoadBlockActive}, isTargetedBySpear: {isTargetedBySpear}";
        public ActivePowerUpsInfo(bool isShieldActive = false, bool isArmorActive = false, bool isInvisibilityActive = false, bool isSpearPowerUpActive = false, bool isStealPowerUpActive = false, bool isRerollPowerUpActive = false, bool isRoadBlockActive = false, bool isTargetedBySpear = false)
        {
            this.isShieldActive = isShieldActive;
            this.isArmorActive = isArmorActive;
            this.isInvisibilityActive = isInvisibilityActive;
            this.isSpearPowerUpActive = isSpearPowerUpActive;
            this.isStealPowerUpActive = isStealPowerUpActive;
            this.isRerollPowerUpActive = isRerollPowerUpActive;
            this.isRoadBlockActive = isRoadBlockActive;
            this.isTargetedBySpear = isTargetedBySpear;
        }
    }

    public struct PortalInfo
    {
        public int Id;
        public RoadChunkType Type;

        public PortalInfo(int id, RoadChunkType type)
        {
            Id = id;
            Type = type;
        }
    }

    public struct TeamTrackProgress
    {
        public int CurrentChunkId;
        public int NextSpecialChunkId;
        public PortalInfo? LastSpecialChunkType;
        public bool IsFinishLineNext;

        public TeamTrackProgress(int currentChunkId = 0, int nextSpecialChunkId = 0, PortalInfo? lastSpecialChunkType = null, bool isFinishLineNext = false)
        {
            CurrentChunkId = currentChunkId;
            NextSpecialChunkId = nextSpecialChunkId;
            LastSpecialChunkType = lastSpecialChunkType;
            IsFinishLineNext = isFinishLineNext;
        }

        public TeamTrackProgress(TeamTrackProgress other, int? currentChunkId = null, int? nextSpecialChunkId = null, PortalInfo? lastSpecialChunkType = null, bool? isFinishLineNext = null)
        {
            CurrentChunkId = currentChunkId ?? other.CurrentChunkId;
            NextSpecialChunkId = nextSpecialChunkId ?? other.NextSpecialChunkId;
            LastSpecialChunkType = lastSpecialChunkType ?? other.LastSpecialChunkType;
            IsFinishLineNext = isFinishLineNext ?? other.IsFinishLineNext;
        }

        public override string ToString() => $"TeamTrackProgress: CurrentChunkId: {CurrentChunkId}, NextSpecialChunkId: {NextSpecialChunkId}, LastSpecialChunkType: {LastSpecialChunkType}, IsFinishLineNext: {IsFinishLineNext}";

        public bool IsEqual(TeamTrackProgress other) => CurrentChunkId == other.CurrentChunkId && NextSpecialChunkId == other.NextSpecialChunkId && LastSpecialChunkType.Equals(other.LastSpecialChunkType) && IsFinishLineNext == other.IsFinishLineNext;
    }

    #endregion

    #region Interfaces

    public interface IRaceNetStateRead //MEMO outdated
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
        SyncVar<bool> RaceTimerExpired_Sub { get; }
        SyncVar<int> FinishLineChunkId_Sub { get; }
    }

    public interface IRaceNetStateStore : IRaceNetStateSubscribe { }

    #endregion

    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceClientProjector))]
    public sealed class RaceNetStateStore : NetworkBehaviour, IRaceNetStateStore
    {

        #region Server-only state and methods
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

        public bool TryGetTeamNob(int teamId, out NetworkObject nob)
        {
            nob = default;
            if (!TryGetTeamData(teamId, out RaceTeamData teamData))
                return false;
            nob = teamData.TeamNob;
            return nob != null;
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

        #endregion

        #region Local Only

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

        //Event Propagation

        public event Action OnRaceTimerExpired;
        private void EventPropagationSetup()
        {
            _raceTimerExpired.OnChange += OnRaceTimerExpired_Propagate;
        }

        private void OnRaceTimerExpired_Propagate(bool prev, bool next, bool asServer)
        {
            if (!prev && next)
                OnRaceTimerExpired?.Invoke();
        }

        #endregion

        #region Networked state and methods

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
        /// <summary> synced </summary>
        private readonly SyncVar<bool> _raceTimerExpired = new(false);
        /// <summary> synced </summary>
        private readonly SyncVar<int> _finishLineChunkId = new(-1);

        public string GetDescription()
        {
            string playerStatesStr = string.Join(", ", PlayerStates.Select(kvp => $"[ConnectionId: {kvp.Key}, State: {kvp.Value}]"));
            string teamDataStr = string.Join(", ", TeamData.Select(kvp => $"[TeamId: {kvp.Key}, Data: {kvp.Value}]"));
            string inventoryStr = string.Join(", ", PlayerInventories.Select(kvp => $"[TeamId: {kvp.Key}, Inventory: {kvp.Value}]"));
            string trackProgressStr = string.Join(", ", TeamTrackProgress.Select(kvp => $"[TeamId: {kvp.Key}, Progress: {kvp.Value}]"));
            string leaderboardStr = string.Join(", ", Leaderboard);
            return $"RaceNetState: Seed: {Seed}, PlayerStates: {playerStatesStr}, TeamData: {teamDataStr}, PlayerInventories: {inventoryStr}, TeamTrackProgress: {trackProgressStr}, Leaderboard: [{leaderboardStr}], RaceTimerExpired: {RaceTimerExpired}";
        }

        // State Projector accessors
        SyncVar<int?> IRaceNetStateSubscribe.Seed_Sub => _seed;
        SyncDictionary<int, RacePlayerState> IRaceNetStateSubscribe.PlayerStates_Sub => _racePlayerStates;
        SyncDictionary<int, RaceTeamData> IRaceNetStateSubscribe.TeamData_Sub => _raceTeamData;
        SyncDictionary<int, InventoryData> IRaceNetStateSubscribe.PlayerInventories_Sub => _racePlayerInventories;
        SyncList<int> IRaceNetStateSubscribe.Leaderboard_Sub => _leaderboard;
        SyncDictionary<int, TeamTrackProgress> IRaceNetStateSubscribe.TeamTrackProgress_Sub => _teamTrackProgress;
        SyncVar<bool> IRaceNetStateSubscribe.RaceTimerExpired_Sub => _raceTimerExpired;
        SyncVar<int> IRaceNetStateSubscribe.FinishLineChunkId_Sub => _finishLineChunkId;

        // State Read-only accessors
        public int? Seed => _seed.Value;
        private ILobbyNetStateRead _lobbyNetStateStore;
        public ILobbyNetStateRead LobbyNetStateStore => _lobbyNetStateStore;
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _racePlayerStates;
        public IReadOnlyDictionary<int, RaceTeamData> TeamData => _raceTeamData;
        public IReadOnlyDictionary<int, InventoryData> PlayerInventories => _racePlayerInventories;
        public IReadOnlyList<int> Leaderboard => _leaderboard;
        public IReadOnlyDictionary<int, TeamTrackProgress> TeamTrackProgress => _teamTrackProgress;
        public bool RaceTimerExpired => _raceTimerExpired.Value;
        public int FinishLineChunkId => _finishLineChunkId.Value;

        [Server]
        public void InitializeFromLobby(ILobbyNetStateRead lobbyState)
        {
            _racePlayerStates.Collection.Clear();
            _raceTeamData.Collection.Clear();
            _leaderboard.Clear();

            _lobbyNetStateStore = lobbyState;

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

            _raceTimerExpired.Value = false;
            _finishLineChunkId.Value = -1;

            EventPropagationSetup();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsHostInitialized)
                EventPropagationSetup();
        }

        #endregion

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
                    inventoryData = new InventoryData(inventoryData, selectedSlot: new PuSlot(1, inventoryData.Slot1.PowerUp));
                    break;
                case 2:
                    inventoryData = new InventoryData(inventoryData, selectedSlot: new PuSlot(2, inventoryData.Slot2.PowerUp));
                    break;
                case 3:
                    inventoryData = new InventoryData(inventoryData, selectedSlot: new PuSlot(3, inventoryData.Slot3.PowerUp));
                    break;
                case 4:
                    inventoryData = new InventoryData(inventoryData, selectedSlot: new PuSlot(4, inventoryData.Slot4.PowerUp));
                    break;
                case 5:
                    inventoryData = new InventoryData(inventoryData, selectedSlot: new PuSlot(5, inventoryData.Slot5.PowerUp));
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

        [Server]
        public void SetTeamTrackProgress(int teamId, TeamTrackProgress trackProgress) => _teamTrackProgress[teamId] = trackProgress;

        [Server]
        public void SetRaceTimerExpired(bool isExpired = true) => _raceTimerExpired.Value = isExpired;

        [Server]
        public void SetTeamInventory(int teamId, InventoryData inventoryData) => _racePlayerInventories[teamId] = inventoryData;

        [Server]
        public void SetFinishLineChunkId(int chunkId)
        {
            if (_finishLineChunkId.Value < 0)
                _finishLineChunkId.Value = chunkId;
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

        public bool TryGetSelectedPowerUp(int teamId, out PuSlot? selectedPowerUp)
        {
            selectedPowerUp = default;
            if (!TryGetTeamInventory(teamId, out InventoryData inventoryData))
                return false;
            selectedPowerUp = inventoryData.SelectedSlot != null && inventoryData.SelectedSlot.Value.PowerUp != PowerUp.None ? inventoryData.SelectedSlot : null;
            return true;
        }


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
            selectedSlot = inventoryData.SelectedSlot?.PowerUp ?? PowerUp.None;
            return true;
        }

        public bool TryGetInventorySelectedSlot(int teamId, out PuSlot selectedSlot)
        {
            selectedSlot = default;
            if (!TryGetTeamInventory(teamId, out InventoryData inventoryData))
                return false;
            if (inventoryData.SelectedSlot.HasValue)
                selectedSlot = inventoryData.SelectedSlot.Value;
            if (selectedSlot.PowerUp == PowerUp.None)
                return false;
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
            if (!TryGetTeamDataByPlayer(p, out RaceTeamData t))
            {
                Log.WLazy(() => $"No team data found for player {p.TeamId}.", this);
                return false;
            }
            if (!TryGetTeamInventory(p.TeamId, out InventoryData i))
            {
                Log.WLazy(() => $"No inventory found for team {p.TeamId}.", this);
                return false;
            }
            playerState = p;
            teamData = t;
            inventory = i;
            return true;
        }

        public bool IsTeamMember(int teamId)
        {
            if (!TryGetTeamDataByPlayerId(LocalConnection.ClientId, out RaceTeamData raceTeamData))
                return false;
            else
                return raceTeamData.TeamId == teamId;
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
