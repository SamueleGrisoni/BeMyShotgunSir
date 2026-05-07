using System;
using BeMyShotgunSir.Scripts.Core.Race;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public interface IPowerUpsNetControllerInitializer
    {
        void Initialize(IPUNCInitContext initContext);
    }

    public interface IPUNCInitContext
    {
        RaceNetController RaceNetController { get; }
        IRaceNetStateRead RaceNetStateRead { get; }
        RaceClientProjector RaceClientProjector { get; }
    }

    public class PowerUpsNetController : NetworkBehaviour, IPowerUpsNetControllerInitializer
    {
        private bool _log = true;
        private bool _isInitialized = false;
        private RaceNetController _raceNetController;
        private IRaceNetStateRead _raceNetStateRead;
        private RaceClientProjector _raceClientProjector;


        public static event Action<IPowerUpsNetControllerInitializer> OnPowerUpsNetControllerSpawned;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnPowerUpsNetControllerSpawned?.Invoke(this);
        }

        public void Initialize(IPUNCInitContext initContext)
        {
            _raceNetController = initContext.RaceNetController;
            _raceNetStateRead = initContext.RaceNetStateRead;
            _raceClientProjector = initContext.RaceClientProjector;
        }
    }
}
