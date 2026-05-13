using System;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.Players
{
    public class ShotgunController : NetworkBehaviour
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<ShotgunController> OnShotgunSpawned;
        public static event Action OnShotgunInitialized;
        public IShotgunInputConsumer _inputConsumer;
        private TeamNetController _teamNetController;
        private PowerUpsNetController _powerUpsNetController;
        private RaceNetStateStore _netState;
        public int? TeamId => _teamNetController == null ? null : _teamNetController.TeamId;
        public void SetName(string name) => transform.name = name;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_teamNetController == null)
            {
                Log.ELazy(() => $"ShotgunController has no TeamNetController on start. TeamId will be null.", this);
                return;
            }
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

        public void SetPowerUpController(PowerUpsNetController powerUpsNetController)
        {
            if (_powerUpsNetController == null)
                _powerUpsNetController = powerUpsNetController;
        }

        [Server] //TODO UI Bind
        private void SetSelectedPowerUp(int slot) => _netState.SetSelectedPowerUp(TeamId.Value, slot);

        [Server] //TODO UI Bind
        private void TriggerAction(PowerUpActionType actionType) => _powerUpsNetController.TriggerAction_ServerRpc(actionType);
    }
}
