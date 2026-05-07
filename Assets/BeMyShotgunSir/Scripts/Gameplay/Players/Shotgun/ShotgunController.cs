using System;
using BeMyShotgunSir.Scripts.UI;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public interface IShotgunController
    {
        void SetInputConsumer(IShotgunInputConsumer inputConsumer);
        void SetTeam(int teamId);
    }

    public class ShotgunController : NetworkBehaviour, IShotgunController
    {
        public static event Action<IShotgunController> OnShotgunSpawned;
        public IShotgunInputConsumer _inputConsumer;
        private int _teamId = -999;

        public void SetInputConsumer(IShotgunInputConsumer inputConsumer)
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

        private void Start()
        {
            _teamId = -999;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
                OnShotgunSpawned?.Invoke(this);
        }
    }
}
