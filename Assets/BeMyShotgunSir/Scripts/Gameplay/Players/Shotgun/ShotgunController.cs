using System;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public interface IShotgunController
    {
        void Initialize(RaceNetContext context, TeamNetController teamNetController);
        int TeamId { get; }
    }

    public class ShotgunController : NetworkBehaviour, IShotgunController
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<IShotgunController> OnShotgunSpawned;
        public IShotgunInputConsumer _inputConsumer;
        private TeamNetController _teamNetController;
        public int TeamId => _teamNetController != null ? _teamNetController.TeamId : (int)Codes.UnInitialized;

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnShotgunSpawned?.Invoke(this);
        }

        public void Initialize(RaceNetContext context, TeamNetController teamNetController)
        {
            if (_isInitialized)
                return;

            if (_inputConsumer == null)
                _inputConsumer = context.InputPublisher;
            _teamNetController = teamNetController;

            _isInitialized = true;
            Log.DLazy(() => $"ShotgunController initialized with teamId {_teamNetController.TeamId}.", this);
        }
    }
}
