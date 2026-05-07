using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    #region DataStructures

    public struct ActivePowerUp
    {
        public int ManagerInstanceId;
        public PowerUp PowerUp;
        public PowerUpClass PowerUpClass;
        public PowerUpState PowerUpState;
        public float TotalDuration;
        public float RemainingDuration;
        public int OwnerTeamId;
        public int TargetTeamId;
    }

    #endregion

    #region Interfaces

    public interface IPowerUpsNetController
    {
        void AddPowerUpToTeam(int teamId, PowerUp powerUpType);
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

        private void Awake()
        {
            TryGetComponent(out _raceNetController);
            TryGetComponent(out _raeNetState);
            TryGetComponent(out _raceClientProjector);

            if (_raceNetController == null || _raeNetState == null || _raceClientProjector == null)
                Log.ELazy(() => "PowerUpsNetController requires RaceNetController, RaceNetStateStore and RaceClientProjector on the same GameObject.", this);
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

        public void UsePowerUp(int teamId, PowerUpRuntime powerUpRuntime)
        {
            if (!RaceNetStateRead.TryGetPlayerInventory(teamId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to use power-up for team {teamId} but no inventory found.", this);
                return;
            }
            if (inventory.SelectedSlot == null)
            {
                Log.WLazy(() => $"Trying to use power-up for team {teamId} but no slot selected.", this);
                return;
            }
            //TODO take from runtimem dictionary the power-up class and start strategy execution
        }

    }
}
