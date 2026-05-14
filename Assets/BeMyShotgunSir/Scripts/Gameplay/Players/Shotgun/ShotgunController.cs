using System;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Messages;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

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
        private DriverController _driverController;
        private PowerUpsNetController _powerUpsNetController;
        private RaceNetStateStore _netState;
        public int? TeamId => _teamNetController == null ? null : _teamNetController.TeamId;
        public void SetName(string name) => transform.name = name;

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnShotgunSpawned?.Invoke(this);
        }

        public void Initialize(RaceNetContext context, TeamNetController teamNetController, DriverController driverController)
        {
            if (_isInitialized)
                return;

            if (_inputConsumer == null)
                _inputConsumer = context.InputPublisher;
            _teamNetController = teamNetController;
            _driverController = driverController;
            _netState = context.NetState;
            if (_inputConsumer != null)
                _inputConsumer.OnWheelMessagePressed += WheelMessagesToDriver;

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
        [ServerRpc]
        private void WheelMessagesToDriver(WheelMessages wheelMessages) => WheelMessagesToDriver_Server(wheelMessages);
        [Server]
        private void WheelMessagesToDriver_Server(WheelMessages wheelMessages)
        {
            if (!TeamId.HasValue)
                return;

            if (_netState.TryGetTeamData(TeamId.Value, out RaceTeamData teamData) && ServerManager.Clients.TryGetValue(teamData.DriverConnectionId, out NetworkConnection conn))
            {
                if (conn != null)
                    Send_WheelMessagesToDriver(conn, wheelMessages);
                else
                    Debug.Log("Connessione non trovata");
            }
        }
        [TargetRpc]
        private void Send_WheelMessagesToDriver(NetworkConnection conn, WheelMessages wheelMessages) => _inputConsumer.SendWheelMessageToDriver(wheelMessages);
    }
}
