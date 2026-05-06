using System;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Gameplay.Players;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region Interfaces

    public interface IRaceManagerInitializer
    {
        void Initialize(LobbyViewModel viewModel, LobbyNetStateStore netState);
    }

    public interface IRaceManager_Bootstrapper : IManager_Bootstrapper
    {
        void BindRace_Initial(IRaceBindTarget[] targets);
    }

    public interface IRaceManager : IManager, IRaceManager_Bootstrapper, IRaceManagerInitializer { }

    #endregion

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
        private ILobbyNetStateRead _lobbyNetStateStore;
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

            if (_netController == null || _netState == null || _projector == null)
                Log.ELazy(() => $"One or more required components are missing on RaceManager.", this);

            _viewModel = new RaceViewModel();
            _raceCommand = new RaceCommand(_netController);
            _binder = new RaceBinder(this);
        }

        private void OnEnable()
        {
            FishNetSceneAdapter.OnSceneInitialized += OnSceneInitialized;
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;
            DriverController.OnDriverSpawned += OnDriverSpawned;
            ShotgunController.OnShotgunSpawned += OnShotgunSpawned;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnRaceManagerSpawned.Invoke(this);
        }

        public void Initialize(LobbyViewModel viewModel, LobbyNetStateStore netState)
        {
            if (_lobbyViewModel != null || _lobbyNetStateStore != null)
            {
                Log.ELazy(() => $"RaceManager is already initialized. Ignoring duplicate initialization.", this);
                return;
            }
            _lobbyViewModel = viewModel;
            _viewModel.InitData(_lobbyViewModel);
            _lobbyNetStateStore = netState;
            _netController.SetLobbyNetState(_lobbyNetStateStore);
            _projector.Init(_viewModel);
            _raceCommand.GetInitSnapshot_Request(); //DANGER
            if (IsServerInitialized)
            {
                _netState.InitializeFromLobby(_lobbyNetStateStore);
                LoadRaceScene(); //DANGER
            }
            Log.DLazy(() => "RaceManager is ready.", this, _log);
            OnRaceManagerInitialized?.Invoke(this);
        }

        [Server]
        private void LoadRaceScene()
        {
            if (!IsServerInitialized) //server instructions below
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
                Log.ELazy(() => $"No bind targets provided for initial bind.", this);
                return;
            }
            _binder.ExecuteInitialBind(targets);
        }

        private void OnSceneInitialized(SceneName name)
        {
            if (name != SceneName.Race)
                return;
            if (IsServerInitialized)
                _netController.InitRace();
        }

        private void OnRoadManagerSpawned(IRoadManager manager)
        {
            if (_roadManager == null)
                _roadManager = manager;
        }

        private void OnDriverSpawned(IDriverController controller)
        {
            _inputPublisher = new InputPublisher();
            controller.SetInputConsumer(_inputPublisher);
            _playerRole = RaceRole.Driver;
            //MEMO here _roadManager and _inputPublisher must be ready
            BindRace_Final(_bindTargets);
        }

        private void OnShotgunSpawned(IShotgunController controller)
        {
            _inputPublisher = new InputPublisher();
            controller.SetInputConsumer(_inputPublisher);
            _playerRole = RaceRole.Shotgun;
            //MEMO here _roadManager and _inputPublisher must be ready
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
                Log.ELazy(() => $"No bind targets provided for final bind.", this);
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
            FishNetSceneAdapter.OnSceneInitialized -= OnSceneInitialized;
            RoadManager.OnRoadManagerSpawned -= OnRoadManagerSpawned;
            DriverController.OnDriverSpawned -= OnDriverSpawned;
            ShotgunController.OnShotgunSpawned -= OnShotgunSpawned;
        }

    }
}
