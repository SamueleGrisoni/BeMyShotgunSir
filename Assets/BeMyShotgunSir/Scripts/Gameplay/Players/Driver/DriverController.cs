using System;
using BeMyShotgunSir.Scripts.UI;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDriverController
    {
        void SetInputConsumer(IDriverInputConsumer inputConsumer);
        void SetTeam(int teamId);
        int GetTeam();
    }
    public class DriverController : NetworkBehaviour, IDriverController
    {
        public static event Action<IDriverController> OnDriverSpawned;
        private IDriverInputConsumer _inputConsumer;
        private int _teamId = -999;
        public void SetInputConsumer(IDriverInputConsumer inputConsumer)
        {
            if (_inputConsumer == null)
                _inputConsumer = inputConsumer;
        }
        public void SetTeam(int teamId)
        {
            if (_teamId != -999)
                return;
            _teamId = teamId;
        }
        public int GetTeam() => _teamId;

        // TODO spostare
    }
}
