using System;
using BeMyShotgunSir.Scripts.UI;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public interface IShotgunController
    {
        void SetInputConsumer(IShotgunInputConsumer inputConsumer);
    }

    public class ShotgunController : NetworkBehaviour, IShotgunController
    {
        public static event Action<IShotgunController> OnShotgunSpawned;
        public IShotgunInputConsumer _inputConsumer;

        public void SetInputConsumer(IShotgunInputConsumer inputConsumer)
        {
            if (_inputConsumer == null)
                _inputConsumer = inputConsumer;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
                OnShotgunSpawned?.Invoke(this);
        }
    }
}
