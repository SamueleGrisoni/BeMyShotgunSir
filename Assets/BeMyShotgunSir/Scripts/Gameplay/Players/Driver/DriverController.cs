using System;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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

        private readonly SyncVar<int?> _syncTeamId = new(null);

        [Server]
        public void SetTeamId(int? teamId) => _syncTeamId.Value = teamId;

        public void SetName(string name) => transform.name = name;

        public void Initialize(RaceNetContext context, TeamNetController teamNetController)
        {
            if (_isInitialized)
                return;

            _inputConsumer = context.InputPublisher;
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
            OnDriverSpawned?.Invoke(this, _syncTeamId.Value);
        }

    }
}
