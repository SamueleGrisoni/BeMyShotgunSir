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

    public interface IRaceManager_Bootstrapper : IManager_Bootstrapper
    {
        void BindRace_Initial(IRaceBindTarget[] targets);
    }

    public interface IRaceManager : IManager, IRaceManager_Bootstrapper { }

    #endregion

    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceNetStateStore))]
    [RequireComponent(typeof(RaceClientProjector))]
    public class RaceManager : NetworkBehaviour, IEventSender, IRaceManager, IRaceBindSources
    {
        //utility
        private bool _log = true;
        string IEventSender.SenderName => name;
        public static event Action<IRaceManager> OnRaceManagerStarted;
        public static event Action<IRaceManager> OnRaceManagerReady;
        public static event Action OnRaceManagerDespawned;

        //binding
        private GameObject _lobbyManager;
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
        private IRoadManager _roadManager;
        private InputPublisher _inputPublisher;
        private RaceRole _playerRole;
        RaceCommand IRaceInitialBindSource.Command => _raceCommand;
        RaceViewModel IRaceInitialBindSource.ViewModel => _viewModel;
        IRoadManager IRaceFinalBindSource.RoadManager => _roadManager;
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

        private void Start()
        {
            Log.DLazy(() => "RaceManager started.", this, _log);
            OnRaceManagerStarted?.Invoke(this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            InitializeFromLobby();
        }

        [Server]
        private void InitializeFromLobby()
        {
            _lobbyManager = GameObject.FindWithTag("LobbyManager");
            if (_lobbyManager == null)
            {
                Log.ELazy(() => $"LobbyManager not found in the scene.", this);
                return;
            }
            _lobbyViewModel = _lobbyManager.GetComponent<LobbyManager>().ViewModel;
            if (_lobbyViewModel == null)
            {
                Log.ELazy(() => $"LobbyViewModel not found on LobbyManager.", this);
                return;
            }
            _viewModel.InitData(_lobbyViewModel);
            _lobbyNetStateStore = _lobbyManager.GetComponent<LobbyNetStateStore>();
            if (_lobbyNetStateStore == null)
            {
                Log.ELazy(() => $"LobbyNetStateStore not found in the scene.", this);
                return;
            }
            _netState.InitializeFromLobby(_lobbyNetStateStore);
            _netController.SetLobbyNetState(_lobbyNetStateStore);

            Log.DLazy(() => "RaceManager is ready.", this, _log);
            OnRaceManagerReady?.Invoke(this);
            LoadRaceScene();
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
            if (!IsController)
                return;

            _projector.Init(_viewModel);
            _raceCommand.GetInitSnapshot_Request();

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
        }

    }
}
