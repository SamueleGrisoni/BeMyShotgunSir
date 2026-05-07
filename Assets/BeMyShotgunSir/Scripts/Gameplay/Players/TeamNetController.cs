using System;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public interface ITeamNetControllerInitializer
    {
        void Initialize(TNCInitContext initContext);
        void InitializeDriver(IDriverController driverController);
        void InitializeShotgun(IShotgunController shotgunController);
    }
    public class TNCInitContext
    {
        public PowerUpsNetController PowerUpsNetController { get; }
        public InputPublisher InputPublisher { get; }
        public IRaceNetStateRead RaceNetStateRead { get; }

        public TNCInitContext(InputPublisher inputPublisher, IRaceNetStateRead raceNetStateRead, PowerUpsNetController powerUpsNetController)
        {
            InputPublisher = inputPublisher;
            RaceNetStateRead = raceNetStateRead;
            PowerUpsNetController = powerUpsNetController;
        }
    }

    public class TeamNetController : NetworkBehaviour, ITeamNetControllerInitializer
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<ITeamNetControllerInitializer> OnTeamSpawned;
        private IDriverController _driverController;
        private IShotgunController _shotgunController;
        private InputPublisher _inputPublisher;
        private RaceRole _assignedRole;


        private void OnEnable()
        {
            DriverController.OnDriverSpawned += InitializeDriver;
            ShotgunController.OnShotgunSpawned += InitializeShotgun;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnTeamSpawned?.Invoke(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            DriverController.OnDriverSpawned -= InitializeDriver;
            ShotgunController.OnShotgunSpawned -= InitializeShotgun;
        }

        public void Initialize(TNCInitContext initContext)
        {
            _inputPublisher = initContext.InputPublisher;
            _assignedRole = initContext.RaceNetStateRead.PlayerStates[LocalConnection.ClientId].Role;
            TryInitializeTeam();
        }

        public void InitializeDriver(IDriverController driverController)
        {
            _driverController = driverController;
            TryInitializeTeam();
        }

        public void InitializeShotgun(IShotgunController shotgunController)
        {
            _shotgunController = shotgunController;
            TryInitializeTeam();
        }

        public bool TryInitializeTeam()
        {
            if (_driverController == null || _shotgunController == null)
                return false;

            if (_assignedRole == RaceRole.Driver)
            {
                _driverController.SetInputConsumer(_inputPublisher);
                Log.ELazy(() => $"Driver initialized and input consumer set.", this);
            }
            else if (_assignedRole == RaceRole.Shotgun)
            {
                _shotgunController.SetInputConsumer(_inputPublisher);
                Log.ELazy(() => $"Shotgun initialized and input consumer set.", this);
            }
            else
            {
                Log.ELazy(() => $"Invalid role assigned to player: {_assignedRole}", this);
                return false;
            }
            return true;
        }

    }
}
