using System;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceManager_Bootstrapper : IManager_Bootstrapper
    {
        /// <summary>
        /// Binds the RaceCommand and RaceViewModel to the given targets. <br/>
        /// </summary>
        /// <param name="targets"></param>
        void BindLobby(IRaceBindTarget[] targets);
        /// <summary>
        /// Sets the TrackManager reference in the RaceManager, allowing it to initialize the track with the correct seed.
        /// </summary>
        /// <param name="trackManager"></param>
        void SetRoadManager(RoadManager trackManager);
    }
    public interface IRaceManager_LobbyManager
    {
        void InitViewModelData(LobbyViewModel viewModel);
    }
    public interface IRaceManager_NetController : IManager_NetController
    {
        void InitRace_Response(int seed, bool isServer = false);
    }
    public interface IRaceManager : IManager, IRaceManager_Bootstrapper, IRaceManager_NetController, IRaceManager_LobbyManager { }


    [RequireComponent(typeof(RaceNetController))]
    public class RaceManager : NetworkBehaviour, IEventSender, IRaceManager
    {
        private bool _log = true;
        public string SenderName => name;
        public static event Action<IRaceManager> OnRaceManagerStarted;
        public static event Action<IRaceManager> OnRaceManagerReady;
        public static event Action OnRaceManagerDespawned;
        private bool _isReady = false;
        private IRaceNetController_Manager _netController;
        private RaceBinder _binder;
        private RaceCommand _raceCommand;
        private RaceViewModel _viewModel;
        private SOAudioRequestEvent _audioRequestEvent;
        private RoadManager _roadManager;
        [SerializeField] private SORaceSounds _sounds;

        public void BindLobby(IRaceBindTarget[] targets)
        {
            if (_binder == null)
            {
                Log.ELazy(() => "No RaceBinder found. Cannot bind race commands.", this);
                return;
            }
            if (_raceCommand == null)
            {
                Log.ELazy(() => "No RaceCommand found. Cannot bind race commands.", this);
                return;
            }
            if (_viewModel == null)
            {
                Log.ELazy(() => "No RaceViewModel found. Cannot bind race data.", this);
                return;
            }
            _binder.Bind(targets);
            _raceCommand.GetInitSnapshot_Request();
        }


        private void Awake()
        {
            _viewModel = new RaceViewModel();
            Debug.Assert(_sounds != null, "SORaceSounds reference is not assigned in the inspector.", this);
        }

        private void OnEnable()
        {
            RaceNetController.OnRaceNetControllerReady += OnRaceNetControllerReady;
            RaceNetController.OnRaceNetControllerDespawned += OnRaceNetControllerDespawned;
            FishNetSceneAdapter.OnSceneInitialized += OnSceneInitialized;
        }

        private void Start()
        {
            Log.DLazy(() => "RaceManager started.", this, _log);
            OnRaceManagerStarted?.Invoke(this);
        }


        public void InitViewModelData(LobbyViewModel viewModel) =>
            _viewModel.InitData(viewModel);


        private void OnRaceNetControllerReady(IRaceNetController netController)
        {
            if (_isReady)
                return;

            if (netController is IRaceNetController_Manager netController_NetController)
                _netController = netController_NetController;
            if (netController is IRaceNetController_Command netController_Command)
                _raceCommand = new RaceCommand(netController_Command);
            _binder = new RaceBinder(_raceCommand, _viewModel);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;

            _isReady = true;
            Log.DLazy(() => "RaceManager is ready.", this, _log);
            OnRaceManagerReady?.Invoke(this);

            if (!IsServerInitialized) //server instructions below
                return;

            GameServices.Instance.SceneCoordinator.LoadRaceScene();
        }

        public void SetRoadManager(RoadManager roadManager) => _roadManager = roadManager;

        private void OnSceneInitialized(SceneName name)
        {
            if (name != SceneName.Race)
                return;
            if (!IsController)
                return;
            _netController.InitRace(_roadManager);
        }

        public void InitRace_Response(int seed, bool isServer = false) =>
            _roadManager.Init(seed, isServer);



        private void OnRaceNetControllerDespawned()
        {
            if (!_isReady)
                return;

            _raceCommand = null;
            _binder = null;
            _audioRequestEvent = null;

            _isReady = false;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            OnRaceNetControllerDespawned();
            UnsubscribeEvents();
            Log.DLazy(() => "RaceManager despawned from the network.", this, _log);
            OnRaceManagerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            RaceNetController.OnRaceNetControllerReady -= OnRaceNetControllerReady;
            RaceNetController.OnRaceNetControllerDespawned -= OnRaceNetControllerDespawned;
            FishNetSceneAdapter.OnSceneInitialized -= OnSceneInitialized;
        }
    }
}
