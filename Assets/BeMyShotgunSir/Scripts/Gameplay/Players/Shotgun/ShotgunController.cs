using System;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public interface IShotgunController
    {
        void SetName(string name);
        void Initialize(RaceNetContext context, TeamNetController teamNetController);
        int? TeamId { get; }
    }

    public class ShotgunController : NetworkBehaviour, IShotgunController
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<IShotgunController, int?> OnShotgunSpawned;
        public IShotgunInputConsumer _inputConsumer;
        private TeamNetController _teamNetController;
        public int? TeamId => _teamNetController == null ? null : _teamNetController.TeamId;
        public void SetName(string name) => transform.name = name;
        private readonly SyncVar<int?> _syncTeamId = new(null);

        [Server]
        public void SetTeamId(int? teamId) => _syncTeamId.Value = teamId;

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnShotgunSpawned?.Invoke(this, _syncTeamId.Value);
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
