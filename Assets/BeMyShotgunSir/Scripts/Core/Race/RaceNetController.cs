using System;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Managing.Server;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceNetController : NetController
    {
        private ServerManager _serverManager;
        private RaceManager _raceManager;
        public event Action OnRaceNetControllerSpawned;
        public event Action OnRaceNetControllerDespawned;

        private void Awake()
        {
            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            if (!TryGetComponent(out _raceManager))
            {
                Log.ELazy(() => "RaceNetController: No RaceManager found on the same GameObject.", this);
            }
        }
    }
}
