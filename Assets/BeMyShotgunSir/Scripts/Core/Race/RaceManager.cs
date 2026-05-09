using System;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Players;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region Interfaces

    public interface IRaceManagerInitializer
    {
        void Initialize(LobbyViewModel viewModel, LobbyNetContext context);
    }

    public interface IRaceManager_Bootstrapper : IManager_Bootstrapper
    {
        void BindRace_Initial(IRaceBindTarget[] targets);
    }

    public interface IRaceManager : IManager, IRaceManager_Bootstrapper, IRaceManagerInitializer { }

    #endregion

    [RequireComponent(typeof(PowerUpsNetController))]
    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceNetStateStore))]
    [RequireComponent(typeof(RaceClientProjector))]
    public class RaceManager : NetworkBehaviour, IEventSender, IRaceManager, IRaceBindSources
    {
        //utility
        private bool _log = true;
        string IEventSender.SenderName => name;
        public static event Action<IRaceManagerInitializer> OnRaceManagerSpawned;
        public static event Action<IRaceManager> OnRaceManagerInitialized;
        public static event Action OnRaceManagerDespawned;

        private LobbyViewModel _lobbyViewModel;
        private LobbyNetStateStore _lobbyNetStateStore;
        private RaceBinder _binder;
        private RaceNetStateStore _netState;
        public IRaceNetStateRead NetState => _netState;
        private RaceNetController _netController;
        private RaceViewModel _viewModel;
        private IRaceBindTarget[] _bindTargets;
        private RaceCommand _raceCommand;
        private RaceClientProjector _projector;
        private IRoadManager _roadManager; //MEMO probably not needed since road manager is sending static events to the UI, but just in case
        private InputPublisher _inputPublisher;
        private RaceRole _playerRole;
        private PowerUpsNetController _powerUpsNetController;

        RaceCommand IRaceInitialBindSource.Command => _raceCommand;
        RaceViewModel IRaceInitialBindSource.ViewModel => _viewModel;
        IRoadManager IRaceFinalBindSource.RoadManager => _roadManager; //MEMO probably not needed since road manager is sending static events to the UI, but just in case
        IInputPublisher IRaceFinalBindSource.InputPublisher => _inputPublisher;
        RaceRole IRaceFinalBindSource.Role => _playerRole;

        private void Awake()
        {
            TryGetComponent(out _netController);
            TryGetComponent(out _netState);
            TryGetComponent(out _projector);
            TryGetComponent(out _powerUpsNetController);

            if (_netController == null || _netState == null || _projector == null || _powerUpsNetController == null)
                Log.ELazy(() => $"One or more required components are missing on RaceManager.", this);
        }

        private void OnEnable()
        {
            RaceSceneBootstrapper.OnRaceBootStrapperAwakened += OnRaceBootStrapperAwakened;
            RaceSceneBootstrapper.OnRaceSceneInitialized += OnRaceSceneInitialized;
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;
            TeamNetController.OnTeamSpawned += OnTeamSpawned;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnRaceManagerSpawned.Invoke(this);
        }

        public void Initialize(LobbyViewModel viewModel, LobbyNetContext context)
        {
            if (_lobbyViewModel != null || _lobbyNetStateStore != null)
            {
                Log.ELazy(() => $"RaceManager is already initialized. Ignoring duplicate initialization.", this);
                return;
            }
            _lobbyViewModel = viewModel;
            _raceCommand = new RaceCommand(_netController);
            _binder = new RaceBinder(this);
            _viewModel = new RaceViewModel(_lobbyViewModel, _netState);
            _lobbyNetStateStore = context.NetState;
            _netController.SetLobbyNetState(_lobbyNetStateStore); //so that net controller can edit lobby net state
            _projector.Init(_viewModel); //so that the projector can change the view model in response of target/observer rpcs
            if (IsServerInitialized)
            {
                _netState.InitializeFromLobby(_lobbyNetStateStore);
                LoadRaceScene();
            }
            Log.DLazy(() => "RaceManager is ready.", this, _log);
            OnRaceManagerInitialized?.Invoke(this);
        }

        private void OnRaceBootStrapperAwakened(IRaceSceneBootstrapperInitializer initializer) => initializer.Initialize(this);

        [Server]
        private void LoadRaceScene()
        {
            if (!IsServerInitialized)
                return;
            GameServices.Instance.SceneCoordinator.LoadRaceScene();
        }

        public void BindRace_Initial(IRaceBindTarget[] targets)
        {
            if (_binder == null || _raceCommand == null || _viewModel == null)
            {
                Log.ELazy(() => $"RaceManager is not fully initialized. Cannot bind race commands. {(_binder == null ? "Binder" : _raceCommand == null ? "Command" : "ViewModel")} is null", this);
                return;
            }
            _bindTargets = targets;
            if (targets == null || targets.Length == 0)
            {
                Log.WLazy(() => $"No bind targets provided for initial bind.", this);
                return;
            }
            _binder.ExecuteInitialBind(targets);
        }

        private void OnRaceSceneInitialized()
        {
            if (IsServerInitialized)
                _netController.InitRace();
        }

        private void OnRoadManagerSpawned(IRoadManager manager)
        {
            if (_roadManager == null)
                _roadManager = manager;
        }

        private void OnTeamSpawned(ITeamNetControllerInitializer initializer)
        {
            var lobbyNetContext = new LobbyNetContext(null, null, _lobbyNetStateStore, null);
            _inputPublisher = new InputPublisher();
            var raceContext = new RaceNetContext(lobbyNetContext, this, _netController, _netState, _projector, _powerUpsNetController, _inputPublisher);
            initializer.Initialize(raceContext, _roadManager);
            BindRace_Final(_bindTargets);
        }

        private void BindRace_Final(IRaceBindTarget[] targets)
        {
            //MEMO since RoadManager is sending static events to the UI
            // having it in the final bind is probably useless. Just keeping it for safety
            if (_binder == null)
            {
                Log.ELazy(() => $"Binder is null. Cannot execute final bind.", this);
                return;
            }
            _binder.UpdateFinalBindSource(this);
            if (targets == null || targets.Length == 0)
            {
                Log.WLazy(() => $"No bind targets provided for final bind.", this);
                return;
            }
            _binder.ExecuteFinalBind(targets);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
            Log.DLazy(() => "RaceManager despawned from the network.", this, _log);
            OnRaceManagerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            RaceSceneBootstrapper.OnRaceBootStrapperAwakened -= OnRaceBootStrapperAwakened;
            RaceSceneBootstrapper.OnRaceSceneInitialized -= OnRaceSceneInitialized;
            RoadManager.OnRoadManagerSpawned -= OnRoadManagerSpawned;
        }

    }
}
