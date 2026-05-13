using System;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public class DriverController : NetworkBehaviour
    {
        //utility
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<DriverController, int?> OnDriverSpawned;

        //CONTEXT
        private RaceNetContext _raceNetContext;
        private IRaceNetController _raceNetController;
        private LobbyNetContext _lobbyNetContext;
        private LobbyNetStateStore _lobbyNetStateStore;
        private RaceNetStateStore _netState;
        private TeamNetController _teamNetController;
        public int? TeamId => _teamNetController == null ? null : _teamNetController.TeamId;

        //specific
        public IDriverInputConsumer _inputConsumer;
        [SerializeField] private MovementController _movementController;
        public Transform GetMovementTransform() => _movementController != null ? _movementController.transform : null;


        [Server]
        public void SetTeamController(TeamNetController teamController)
        {
            if (_teamNetController == null)
                _teamNetController = teamController;

            _netState.SetTeamTrackProgress(TeamId.Value, new TeamTrackProgress(0, 0, new PortalInfo(0, Track.RoadChunkType.START_LINE)));
        }

        public void SetName(string name) => transform.name = name;

        public void Initialize(RaceNetContext context, TeamNetController teamNetController)
        {
            if (_isInitialized)
                return;

            _inputConsumer = context.InputPublisher;
            _movementController.Initialize(new RaceNetContext(null, null, null, context.NetState, context.ClientProjector, context.PowerUpsNetController, null), _inputConsumer);
            _teamNetController = teamNetController;

            if (_inputConsumer == null || _teamNetController == null)
                Log.ELazy(() => $"DriverController initialized with missing parameters.", this);
            else
                Log.DLazy(() => $"DriverController initialized with teamId {_teamNetController.TeamId}.", this, _log);

            _isInitialized = true;
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_teamNetController == null)
            {
                Log.ELazy(() => $"DriverController has no TeamNetController on start. TeamId will be null.", this);
                return;
            }
            OnDriverSpawned?.Invoke(this, _teamNetController.TeamId);
        }
    }
}
