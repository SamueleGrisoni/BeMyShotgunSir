using System;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDriverController
    {
        void InitializeDriver(RaceNetContext context, TeamNetController teamNetController);
        int TeamId { get; }
        Transform GetTransform();
    }
    public class DriverController : NetworkBehaviour, IDriverController
    {
        //utility
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<IDriverController> OnDriverSpawned;

        //CONTEXT
        private RaceNetContext _raceNetContext;
        private IRaceNetController _raceNetController;
        private LobbyNetContext _lobbyNetContext;
        private LobbyNetStateStore _lobbyNetStateStore;
        private RaceNetStateStore _netState;
        private TeamNetController _teamNetController;
        public int TeamId => _teamNetController != null ? _teamNetController.TeamId : (int)Codes.UnInitialized;

        //specific
        private IDriverInputConsumer _inputConsumer;
        public Transform GetTransform() => transform;
        public void InitializeDriver(RaceNetContext context, TeamNetController teamNetController)
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
            OnDriverSpawned?.Invoke(this);
        }
    }
}
