using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    #region DataStructures

    public struct InfoUsePowerUp
    {
        public int OwnerTeamId;
        public PowerUp PowerUp;
        public int TargetTeamId;
    }

    public struct ActivePowerUp
    {
        public int OwnerTeamId;
        public PowerUp PowerUp;
        public int TargetTeamId;
        public int ManagerInstanceId;
        public PowerUpClass PowerUpClass;
        public PowerUpState PowerUpState;
        public float TotalDuration;
        public float RemainingDuration;

        public ActivePowerUp(InfoUsePowerUp info, int managerInstanceId, SOPowerUp definition, PowerUpState? powerUpState = PowerUpState.None, float? remainingDuration = -1)
        {
            OwnerTeamId = info.OwnerTeamId;
            PowerUp = info.PowerUp;
            TargetTeamId = info.TargetTeamId;
            ManagerInstanceId = managerInstanceId;
            PowerUpClass = definition.PowerUpClass;
            PowerUpState = powerUpState ?? PowerUpState.None;
            TotalDuration = definition.Duration;
            RemainingDuration = remainingDuration ?? definition.Duration;
        }
    }

    #endregion

    #region Interfaces

    public interface IPowerUpsNetController
    {
        void AddPowerUpToTeam(int teamId, PowerUp powerUpType);
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
        private RaceNetStateStore _raeNetState;
        public IRaceNetStateRead RaceNetStateRead => _raeNetState;
        private RaceClientProjector _raceClientProjector;
        private StrategyContext _strategyContext;


        [SerializeField] private SOPowerUpsData _powerUpData;
        private Dictionary<PowerUp, SOPowerUp> _definitions;
        public IReadOnlyDictionary<PowerUp, SOPowerUp> Definitions => _definitions;
        private int _nextPowerUpInstanceId = 0;
        public int NextPowerUpInstanceId => _nextPowerUpInstanceId++;
        private Dictionary<int, PowerUpRuntime> _activePowerUps;

        private float _tickTimer = 0;
        [SerializeField] private float _tICK_INTERVAL = 0.3f;

        private void Awake()
        {
            TryGetComponent(out _raceNetController);
            TryGetComponent(out _raeNetState);
            TryGetComponent(out _raceClientProjector);

            if (_raceNetController == null || _raeNetState == null || _raceClientProjector == null)
                Log.ELazy(() => "PowerUpsNetController requires RaceNetController, RaceNetStateStore and RaceClientProjector on the same GameObject.", this);

            _activePowerUps = new Dictionary<int, PowerUpRuntime>();
            _definitions = _powerUpData.GetPowerUpDefinitions();
            _strategyContext = new StrategyContext(_raeNetState, this, _activePowerUps);

        }

        public void OnEnable() => PowerUpSpawnable.OnPowerUpSpawned += OnPowerUpSpawned;

        public void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents() => PowerUpSpawnable.OnPowerUpSpawned -= OnPowerUpSpawned;

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        public void OnPowerUpSpawned(IPowerUpSpawnable powerUpSpawnable) =>
            powerUpSpawnable.Initialize(this);

        public void AddPowerUpToTeam(int teamId, PowerUp powerUpType)
        {
            if (!RaceNetStateRead.TryGetPlayerInventory(teamId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to add power-up to team {teamId} but no inventory found.", this);
                return;
            }
            if (inventory.IsFull)
            {
                Log.WLazy(() => $"Trying to add power-up to team {teamId} but inventory is full.", this);
                return;
            }

            if (inventory.Slot1 == PowerUp.None)
                inventory.Slot1 = powerUpType;
            else if (inventory.Slot2 == PowerUp.None)
                inventory.Slot2 = powerUpType;
            else if (inventory.Slot3 == PowerUp.None)
                inventory.Slot3 = powerUpType;
            else if (inventory.Slot4 == PowerUp.None)
                inventory.Slot4 = powerUpType;
            else if (inventory.Slot5 == PowerUp.None)
                inventory.Slot5 = powerUpType;

            _raeNetState.SetPlayerInventory(teamId, inventory);
            Log.DLazy(() => $"Added power-up {powerUpType} to team {teamId}. Inventory now: {inventory}", this, _log);
        }

        public void ActivatePowerUp(InfoUsePowerUp info)
        {
            if (!_definitions.TryGetValue(info.PowerUp, out SOPowerUp definition))
            {
                Log.WLazy(() => $"Trying to activate power-up {info.PowerUp} for team {info.OwnerTeamId} but no definition found.", this);
                return;
            }
            if (definition.TryPreliminaryCheck(info, _strategyContext))
            {
                PowerUpRuntime powerUpRuntime = definition.OnCreateRuntime(definition, info, _strategyContext);
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

        public void SetPowerUpTarget(int instanceId, int targetTeamId)
        {
            if (!_activePowerUps.TryGetValue(instanceId, out PowerUpRuntime powerUpRuntime))
            {
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} but no active power-up found with this id.", this);
                return;
            }
            NetworkObject targetNob = null;
            if (!_raeNetState.TryGetTeamNob(targetTeamId, out targetNob))
                Log.WLazy(() => $"Trying to change target of power-up instance {instanceId} to team {targetTeamId} but no nob found for this team.", this);

            _activePowerUps[instanceId].Definition.OnChangeTarget(powerUpRuntime, instanceId, targetTeamId, _strategyContext);
        }

        private void Update()
        {
            if (!IsServerInitialized)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _tICK_INTERVAL)
            {
                _tickTimer = 0;
                foreach (KeyValuePair<int, PowerUpRuntime> kvp in _activePowerUps)
                {
                    PowerUpRuntime powerUpRuntime = kvp.Value;
                    powerUpRuntime.Definition.OnTick(_tICK_INTERVAL, powerUpRuntime, _strategyContext);
                }
            }
        }

    }
}
