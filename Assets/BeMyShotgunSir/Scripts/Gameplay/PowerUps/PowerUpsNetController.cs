using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    #region DataStructures

    public struct InfoUsePowerUp
    {
        public int OwnerTeamId;
        public PowerUp PowerUp;
        public int? TargetTeamId;
        public override string ToString() => $"OwnerTeamId: {OwnerTeamId}, PowerUp: {PowerUp}, TargetTeamId: {TargetTeamId}";
        public InfoUsePowerUp(int ownerTeamId, PowerUp powerUp, int? targetTeamId = null)
        {
            OwnerTeamId = ownerTeamId;
            PowerUp = powerUp;
            TargetTeamId = targetTeamId;
        }
    }

    public struct ActivePowerUp
    {
        public int OwnerTeamId;
        public PowerUp PowerUp;
        public int? TargetTeamId;
        public int ManagerInstanceId;
        public PowerUpClass PowerUpClass;
        public PowerUpState PowerUpState;
        public float TotalDuration;
        public float RemainingDuration;

        public ActivePowerUp(InfoUsePowerUp info, int managerInstanceId, SOPowerUp definition, PowerUpState? powerUpState = PowerUpState.None)
        {
            OwnerTeamId = info.OwnerTeamId;
            PowerUp = info.PowerUp;
            TargetTeamId = info.TargetTeamId;
            ManagerInstanceId = managerInstanceId;
            PowerUpClass = definition.PowerUpClass;
            PowerUpState = powerUpState ?? PowerUpState.None;
            TotalDuration = definition.Duration;
            RemainingDuration = definition.Duration;
        }
    }

    public class SpawnedPowerUpData
    {
        public PowerUpSpawnable reference;
        public int ChunkIndex;
        public SpawnedPowerUpData(PowerUpSpawnable reference, int chunkIndex)
        {
            this.reference = reference;
            ChunkIndex = chunkIndex;
        }
    }

    #endregion

    #region Interfaces

    public interface IPowerUpsNetController
    {
        bool AddPowerUpToTeam(int teamId, PowerUp powerUpType);
    }

    public class StrategyContext
    {
        public RaceNetStateStore RaceNetStateStore { get; }
        public PowerUpsNetController PowerUpsNetController { get; }
        public Dictionary<int, PowerUpRuntime> ActivePowerUps { get; }
        public StrategyContext(RaceNetStateStore raceNetStateStore, PowerUpsNetController powerUpsNetController, Dictionary<int, PowerUpRuntime> activePowerUps)
        {
            RaceNetStateStore = raceNetStateStore;
            PowerUpsNetController = powerUpsNetController;
            ActivePowerUps = activePowerUps;
        }
    }

    #endregion

    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceNetStateStore))]
    [RequireComponent(typeof(RaceClientProjector))]
    public class PowerUpsNetController : NetworkBehaviour, IPowerUpsNetController
    {
        private bool _log = true;
        private bool _isInitialized = false;
        private RaceNetController _raceNetController;
        private RaceNetStateStore _raceNetState;
        public IRaceNetStateRead RaceNetStateRead => _raceNetState;
        private RaceClientProjector _raceClientProjector;
        private StrategyContext _strategyContext;

        [SerializeField] private SOPowerUpsData _powerUpData;
        private Dictionary<PowerUp, SOPowerUp> _definitions;
        public IReadOnlyDictionary<PowerUp, SOPowerUp> Definitions => _definitions;
        private int _nextPowerUpInstanceId = 0;
        private Dictionary<int, PowerUpRuntime> _activePowerUps;
        private List<SpawnedPowerUpData> _spawnedPowerUps = new List<SpawnedPowerUpData>();

        private float _tickTimer = 0;
        [SerializeField] private float _tICK_INTERVAL = 0.3f;

        private void Awake()
        {
            TryGetComponent(out _raceNetController);
            TryGetComponent(out _raceNetState);
            TryGetComponent(out _raceClientProjector);

            if (_raceNetController == null || _raceNetState == null || _raceClientProjector == null)
                Log.ELazy(() => "PowerUpsNetController requires RaceNetController, RaceNetStateStore and RaceClientProjector on the same GameObject.", this);

            _activePowerUps = new Dictionary<int, PowerUpRuntime>();
            _definitions = _powerUpData.GetPowerUpDefinitions();
            _strategyContext = new StrategyContext(_raceNetState, this, _activePowerUps);
        }

        public void OnEnable() => PowerUpSpawnable.OnPowerUpSpawned += OnPowerUpSpawned;

        public void DebugUpdate() //DEBUG
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                Log.DLazy(() => $"Current active power-ups: {string.Join(", ", _activePowerUps.Values)}", this, _log);
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                Log.DLazy(() => $"Current spawned power-ups: {string.Join(", ", _spawnedPowerUps)}", this, _log);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                Log.DLazy(() => $"Current race net state: {_raceNetState.GetDescription()}", this, _log);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.Armor);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.Invisibility);

            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.RerollPowerUp);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.RoadBlock);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.Shield);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.Spear);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                DebugAddPowerUpToTeam_ServerRpc(LocalConnection.ClientId, PowerUp.StealPowerUp);
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                _raceNetState.TryGetTeamDataByPlayerId(LocalConnection.ClientId, out RaceTeamData teamData);
                _raceNetState.TryGetInventorySelectedSlot(teamData.TeamId, out PowerUp selectedPowerUp);
                Log.ELazy(() => $"Selected power-up: {selectedPowerUp}", this);
                ActivatePowerUp(new InfoUsePowerUp(teamData.TeamId, selectedPowerUp));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DebugAddPowerUpToTeam_ServerRpc(int clientId, PowerUp powerUp)
        {
            _raceNetState.TryGetTeamDataByPlayerId(clientId, out RaceTeamData teamData);
            AddPowerUpToTeam(teamData.TeamId, powerUp);
            Log.ELazy(() => $"Added {powerUp} power-up to team {teamData.TeamId} for testing.", this);

        }

        public void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents() => PowerUpSpawnable.OnPowerUpSpawned -= OnPowerUpSpawned;

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        [Server]
        public void OnPowerUpSpawned(PowerUpSpawnable powerUpSpawnable)
        {
            _spawnedPowerUps.Add(new SpawnedPowerUpData(powerUpSpawnable, powerUpSpawnable.ChunkIndex ?? -1));
            powerUpSpawnable.Initialize(this, _raceNetState);
        }

        [Server]
        public void DespawnSurpassedPowerUp(int chunkIndex) //TODO this should be called whenever all the players surpass a special chunk
        {
            List<SpawnedPowerUpData> toDespawn = _spawnedPowerUps.FindAll(data => data.ChunkIndex < chunkIndex);
            foreach (SpawnedPowerUpData data in toDespawn)
            {
                if (data.reference != null && data.reference.IsSpawned)
                {
                    data.reference.Despawn();
                    Log.DLazy(() => $"Despawning power-up {data.reference.name} in chunk {data.ChunkIndex} because it has been surpassed by all players.", this, _log);
                }
                else
                {
                    Log.WLazy(() => $"Trying to despawn power-up in chunk {data.ChunkIndex} but reference is null or not spawned.", this, _log);
                }
            }
            _spawnedPowerUps.RemoveAll(data => data.ChunkIndex < chunkIndex);
        }

        [Server]
        public int GetInstanceId() => _nextPowerUpInstanceId++;

        [Server]
        public void AddActivePowerUp(PowerUpRuntime powerUpRuntime)
        {
            _activePowerUps[powerUpRuntime.ActivePowerUpData.ManagerInstanceId] = powerUpRuntime;
            if (powerUpRuntime.Definition.PowerUpClass == PowerUpClass.TimeBased)
                Log.DLazy(() => $"Added time-based power-up instance {powerUpRuntime.ActivePowerUpData.ManagerInstanceId} of type {powerUpRuntime.Definition.PowerUpType} for team {powerUpRuntime.ActivePowerUpData.OwnerTeamId}. Total duration: {powerUpRuntime.ActivePowerUpData.TotalDuration}, Remaining duration: {powerUpRuntime.ActivePowerUpData.RemainingDuration}.", this, _log);
            if (powerUpRuntime.Definition.PowerUpClass == PowerUpClass.OneShot)
                Log.DLazy(() => $"Added active one-shot power-up instance {powerUpRuntime.ActivePowerUpData.ManagerInstanceId} of type {powerUpRuntime.Definition.PowerUpType} for team {powerUpRuntime.ActivePowerUpData.OwnerTeamId}.", this, _log);
            if (powerUpRuntime.Definition.PowerUpClass == PowerUpClass.ActionBased)
                Log.DLazy(() => $"Added active action-based power-up instance {powerUpRuntime.ActivePowerUpData.ManagerInstanceId} of type {powerUpRuntime.Definition.PowerUpType} for team {powerUpRuntime.ActivePowerUpData.OwnerTeamId}.", this, _log);
        }

        [Server]
        public void RemoveActivePowerUp(int instanceId)
        {
            if (_activePowerUps.TryGetValue(instanceId, out PowerUpRuntime powerUpRuntime))
            {
                _activePowerUps.Remove(instanceId);
                Log.DLazy(() => $"Removed active power-up instance {instanceId} of type {powerUpRuntime.Definition.PowerUpType} for team {powerUpRuntime.ActivePowerUpData.OwnerTeamId}.", this, _log);
            }
            else
            {
                Log.WLazy(() => $"Trying to remove active power-up instance {instanceId} but no active power-up found with this id.", this, _log);
            }
        }

        private void Update()
        {
            DebugUpdate(); //DEBUG

            if (!IsServerInitialized)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _tICK_INTERVAL)
            {
                _tickTimer = 0;
                var activePowerUpsSnapshot = new Dictionary<int, PowerUpRuntime>(_activePowerUps);
                foreach (KeyValuePair<int, PowerUpRuntime> kvp in activePowerUpsSnapshot)
                {
                    PowerUpRuntime powerUpRuntime = kvp.Value;
                    powerUpRuntime.Definition.OnTick(_tICK_INTERVAL, powerUpRuntime, _strategyContext);
                }
            }
        }

        [Server]
        public bool AddPowerUpToTeam(int teamId, PowerUp powerUpType)
        {
            if (!RaceNetStateRead.TryGetTeamInventory(teamId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to add power-up to team {teamId} but no inventory found.", this);
                return false;
            }
            if (inventory.IsFull)
            {
                Log.WLazy(() => $"Trying to add power-up to team {teamId} but inventory is full.", this);
                return false;
            }
            inventory = inventory.SetFreeSlot(powerUpType);
            _raceNetState.SetPlayerInventory(teamId, inventory);
            Log.DLazy(() => $"Added power-up {powerUpType} to team {teamId}. Inventory now: {inventory}", this, _log);
            _raceNetState.TryGetShotgunNob(teamId, out NetworkObject shotgunNob);
            _raceNetState.TryGetTeamShotgunConnectionId(teamId, out int connectionId);
            if (shotgunNob != null)
                shotgunNob.GetComponent<ShotgunController>().PickUpPowerUp_TargetRpc(_raceNetState.LobbyNetStateStore.PlayerStates[connectionId].Connection);
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void EquipPowerUp_ServerRpc(int slotIndex, NetworkConnection connection = null)
        {
            if (_raceNetState.TryUnwrapConnectionState(connection.ClientId, out RacePlayerState playerState, out RaceTeamData teamData, out InventoryData inventory))
            {
                if (slotIndex < 0 || slotIndex > inventory.MaxPowerUps)
                {
                    Log.WLazy(() => $"Trying to equip power-up for client {connection.ClientId} with invalid slot index {slotIndex}.", this);
                    return;
                }
                PowerUp selectedPowerUp = inventory.GetPowerUpInSlot(slotIndex);
                if (selectedPowerUp == PowerUp.None)
                {
                    Log.WLazy(() => $"Trying to equip power-up for client {connection.ClientId} with empty slot index {slotIndex}.", this);
                    return;
                }
                _raceNetState.SetSelectedPowerUp(playerState.TeamId, slotIndex);
                Log.DLazy(() => $"Client {connection.ClientId} equipped power-up {selectedPowerUp} in slot {slotIndex}.", this, _log);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ActivatePowerUp_ServerRpc(NetworkConnection connection = null) //TODO expose command to ui
        {
            if (_raceNetState.TryUnwrapConnectionState(connection.ClientId, out RacePlayerState playerState, out RaceTeamData teamData, out InventoryData inventory))
            {
                if (!inventory.SelectedSlot.HasValue || inventory.SelectedSlot.Value.PowerUp == PowerUp.None)
                {
                    Log.WLazy(() => $"Trying to use power-up for team {playerState.TeamId} but no slot selected.", this);
                    return;
                }
                var powerUpInfo = new InfoUsePowerUp(playerState.TeamId, inventory.SelectedSlot.HasValue ? inventory.SelectedSlot.Value.PowerUp : PowerUp.None);
                if (IsUsingMaxNumberOfPowerUps(teamData))
                {
                    Log.WLazy(() => $"Trying to use power-up {powerUpInfo.PowerUp} for team {powerUpInfo.OwnerTeamId} but already using max number of power-ups.", this);
                    return;
                }
                ActivatePowerUp(powerUpInfo);
            }
        }

        private bool IsUsingMaxNumberOfPowerUps(RaceTeamData teamData)
        {
            int activePowerUpsCount = 0;
            if (teamData.ActivePowerUps == null || teamData.ActivePowerUps.Length == 0)
            {
                Log.WLazy(() => $"Cannot check active power-ups for team {teamData.TeamId}.", this);
                return false;
            }

            foreach (PowerUpIdentifier powerUp in teamData.ActivePowerUps)
            {
                if (powerUp.PowerUp != PowerUp.None)
                    activePowerUpsCount++;
            }
            return activePowerUpsCount >= BMMSDefaults.MAX_ACTIVE_POWER_UPS;
        }

        [Server]
        public void ActivatePowerUp(InfoUsePowerUp info)
        {
            if (!_definitions.TryGetValue(info.PowerUp, out SOPowerUp definition))
            {
                Log.WLazy(() => $"Trying to activate power-up {info.PowerUp} for team {info.OwnerTeamId} but no definition found.", this);
                return;
            }
            if (definition.TryPreliminaryCheck(info, _strategyContext))
            {
                PowerUpRuntime powerUpRuntime = definition.OnCreateRuntime(info, _strategyContext);
                if (definition.CanActivate(powerUpRuntime, _strategyContext))
                {
                    definition.OnActivate(powerUpRuntime, _strategyContext);
                    Log.DLazy(() => $"Team {info.OwnerTeamId} activated power-up {info.PowerUp}. Current target: {powerUpRuntime.TargetNob?.name ?? "None"}", this, _log);
                }
            }
            else
            {
                Log.WLazy(() => $"Team {info.OwnerTeamId} tried to activate power-up {info.PowerUp} but preliminary conditions were not met.", this);
            }
        }

        [Server]
        public void SetPowerUpTarget_ServerRpc(int instanceId, int targetTeamId, NetworkConnection connection = null)
        {
            if (!_activePowerUps.TryGetValue(instanceId, out PowerUpRuntime powerUpRuntime))
            {
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} but no active power-up found with this id.", this);
                return;
            }
            _raceNetState.TryGetTeamData(powerUpRuntime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            if (!teamData.HasActivePowerUp(powerUpRuntime.ActivePowerUpData.PowerUp))
            {
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} but the owner team {powerUpRuntime.ActivePowerUpData.OwnerTeamId} is not using this power-up anymore.", this);
                return;
            }
            if (!_raceNetState.TryGetTeamData(targetTeamId, out RaceTeamData targetTeamData))
            {
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} to team {targetTeamId} but no data found for this team.", this);
                return;
            }
            if (targetTeamData.IsTargetedByPowerUp(powerUpRuntime.ActivePowerUpData.PowerUp))
            {
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} to team {targetTeamId} but this team is already targeted by the same power-up.", this);
                return;
            }
            if (targetTeamData.ActivePowerUpInfo.isShieldActive)
            {
                Log.WLazy(() => $"Target team {targetTeamId} has shield power up", this);
                return;
            }
            SetPowerUpTarget(instanceId, targetTeamId);
        }

        [Server]
        public int? GetInstanceId(int teamId, PowerUp type)
        {
            if (!_raceNetState.TryGetTeamData(teamId, out RaceTeamData teamData))
            {
                Log.WLazy(() => $"Trying to get instance id of power-up {type} for team {teamId} but no data found for this team.", this);
                return null;
            }
            if (teamData.ActivePowerUps == null || teamData.ActivePowerUps.Length == 0)
            {
                Log.WLazy(() => $"Trying to get instance id of power-up {type} for team {teamId} but this team has no active power-ups.", this);
                return null;
            }
            foreach (PowerUpIdentifier powerUpIdentifier in teamData.ActivePowerUps)
            {
                if (powerUpIdentifier.PowerUp == type)
                {
                    if (!_activePowerUps.TryGetValue(powerUpIdentifier.InstanceId, out PowerUpRuntime powerUpRuntime))
                        continue;
                    Log.DLazy(() => $"Found instance id {powerUpIdentifier.InstanceId} for power-up {type} of team {teamId}.", this);
                    return powerUpIdentifier.InstanceId;
                }
            }
            return null;
        }

        [Server]
        public void SetPowerUpTarget(int instanceId, int targetTeamId)
        {
            if (!_activePowerUps.TryGetValue(instanceId, out PowerUpRuntime powerUpRuntime)) //redundant check but better safe than sorry
            {
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} but no active power-up found with this id.", this);
                return;
            }
            NetworkObject targetNob = null;
            if (!_raceNetState.TryGetTeamNob(targetTeamId, out targetNob))
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} to team {targetTeamId} but no nob found for this team.", this);

            _activePowerUps[instanceId].Definition.OnChangeTarget(powerUpRuntime, instanceId, targetTeamId, _strategyContext);
        }

        [Server]
        public void TriggerAction_ServerRpc(PowerUpAction action)
        {
            if (!_activePowerUps.TryGetValue(action.Identifier.InstanceId, out PowerUpRuntime powerUpRuntime))
            {
                Log.WLazy(() => $"Trying to trigger action {action.ActionType} for power-up instance {action.Identifier.InstanceId} but no active power-up found with this id.", this);
                return;
            }
            _activePowerUps[action.Identifier.InstanceId].Definition.OnAction(powerUpRuntime, _strategyContext, action);
        }

        [Server]
        public void TriggerAction_ServerRpc(PowerUpActionType actionType, NetworkConnection connection = null)
        {
            if (!_raceNetState.TryGetTeamInventory(connection.ClientId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to trigger action {actionType} for client {connection.ClientId} but no inventory found.", this);
                return;
            }
            if (actionType == PowerUpActionType.Aim && (!inventory.SelectedSlot.HasValue || inventory.SelectedSlot.Value.PowerUp == PowerUp.None))
            {
                Log.WLazy(() => $"Trying to trigger action {actionType} for client {connection.ClientId} but no power-up selected.", this);
                return;
            }
            PowerUp selectedPowerUp = inventory.SelectedSlot.Value.PowerUp;
            if (!_raceNetState.TryGetTeamData(connection.ClientId, out RaceTeamData teamData))
            {
                Log.WLazy(() => $"Trying to trigger action {actionType} for client {connection.ClientId} but no team data found.", this);
                return;
            }
            if (teamData.ActivePowerUps == null || teamData.ActivePowerUps.Length == 0)
            {
                Log.WLazy(() => $"Trying to trigger action {actionType} for client {connection.ClientId} but team {teamData.TeamId} has no active power-ups.", this);
                return;
            }
            if (teamData.HasActivePowerUp(selectedPowerUp))
            {
                foreach (PowerUpIdentifier powerUpIdentifier in teamData.ActivePowerUps)
                {
                    if (powerUpIdentifier.PowerUp == selectedPowerUp)
                    {
                        if (!_activePowerUps.TryGetValue(powerUpIdentifier.InstanceId, out PowerUpRuntime powerUpRuntime))
                            continue;
                        _activePowerUps[powerUpIdentifier.InstanceId].Definition.OnAction(powerUpRuntime, _strategyContext, new PowerUpAction(actionType, powerUpIdentifier, teamData.TeamId));
                    }
                }
            }
            else
            {
                Log.WLazy(() => $"Trying to trigger action {actionType} for client {connection.ClientId} but the selected power-up {selectedPowerUp} is not active for this team.", this);
                return;
            }
        }
    }
}
