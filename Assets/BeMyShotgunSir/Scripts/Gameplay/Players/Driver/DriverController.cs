using System;
using BeMyShotgunSir.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class DriverController : NetworkBehaviour
    {
        //utility
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<DriverController> OnDriverSpawned;

        //CONTEXT
        private RaceNetContext _raceNetContext;
        private IRaceNetController _raceNetController;
        private LobbyNetContext _lobbyNetContext;
        private LobbyNetStateStore _lobbyNetStateStore;
        private RaceNetStateStore _netState;
        private TeamNetController _teamNetController;
        private ShotgunController _shotgunController;
        public int? TeamId => _teamNetController == null ? null : _teamNetController.TeamId;

        //specific
        public IDriverInputConsumer _inputConsumer;
        [SerializeField] private MovementController _movementController;
        public Transform GetMovementTransform() => _movementController != null ? _movementController.transform : null;

        public void SetName(string name) => transform.name = name;

        public void Initialize(RaceNetContext context, TeamNetController teamNetController, ShotgunController shotgunController)
        {
            if (_isInitialized)
                return;
            _shotgunController = shotgunController;
            _inputConsumer = context.InputPublisher;
            _movementController.Initialize(new RaceNetContext(null, null, null, context.NetState, context.ClientProjector, context.PowerUpsNetController, null), _inputConsumer, this);
            _teamNetController = teamNetController;
            _netState = context.NetState;

            if (_inputConsumer == null || _teamNetController == null)
                Log.ELazy(() => $"DriverController initialized with missing parameters.", this);
            else
                Log.DLazy(() => $"DriverController initialized with teamId {_teamNetController.TeamId}.", this, _log);

            _isInitialized = true;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnDriverSpawned?.Invoke(this);
        }

        [ObserversRpc(BufferLast = true)]
        public void ApplyInvisibilityEffect(bool isApplying)
        {
            if (!TeamId.HasValue)
            {
                Log.ELazy(() => $"Trying to apply Invisibility effect for driver with no team. Ignoring.", this);
                return;
            }
            bool isMemberOfTeam = _netState.IsTeamMember(TeamId.Value);
            bool shouldHide = isApplying && !isMemberOfTeam;
            transform.GetComponentInChildren<DriverVisuals>()._visualModel.gameObject.SetActive(!shouldHide);
        }

        [TargetRpc]
        public void ActivateControls_TargetRpc(NetworkConnection connection) => Log.DLazy(() => $"Activating controls for driver on client {connection.ClientId}.", this, _log); //TODO
    }
}
