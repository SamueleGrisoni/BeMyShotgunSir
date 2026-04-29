using System;
using BeMyShotgunSir.Scripts.Events;
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
    }
    public interface IRaceManager_NetController : IManager_NetController { }
    public interface IRaceManager : IManager, IRaceManager_Bootstrapper, IRaceManager_NetController { }


    [RequireComponent(typeof(RaceNetController))]
    public class RaceManager : NetworkBehaviour, IEventSender, IRaceManager
    {
        public string SenderName => name;
        public static event Action<IRaceManager> OnRaceManagerSpawned;
        public static event Action<IRaceManager> OnRaceManagerReady;
        private bool _isReady = false;
        public static event Action OnRaceManagerDespawned;

        private RaceBinder _binder;
        private RaceCommand _raceCommand;
        private RaceViewModel _viewModel;
        [SerializeField] private RaceNetController _netController;

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

        [SerializeField] private SORaceSounds _sounds;
        private SOAudioRequestEvent _audioRequestEvent;

        private void Awake()
        {
            Debug.Assert(_netController != null, "RaceNetController reference is not assigned in the inspector.", this);
            Debug.Assert(_sounds != null, "SORaceSounds reference is not assigned in the inspector.", this);

            _viewModel = new RaceViewModel();

            if (TryGetComponent(out _netController))
            {
                _netController.OnRaceNetControllerSpawned += HandleRaceNetControllerSpawned;
                _netController.OnRaceNetControllerDespawned += HandleRaceNetControllerDespawned;
            }
            FishNetSceneAdapter.OnSceneInitialized += HandleSceneInitialized;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Log.DLazy(() => "RaceManager spawned on the network.", this);
        }

        private void HandleRaceNetControllerSpawned()
        {
            if (_isReady)
                return;

            _raceCommand = new RaceCommand(_netController);
            _binder = new RaceBinder(_raceCommand, _viewModel);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            _viewModel.InitData();

            _isReady = true;
            OnRaceManagerReady?.Invoke(this);

            if (!IsServerInitialized) //server instructions below
                return;

            GameServices.Instance.SceneCoordinator.LoadRaceScene();
        }

        private void HandleSceneInitialized(SceneName name)
        {
            if (name != SceneName.Race)
                return;


        }

        private void HandleRaceNetControllerDespawned()
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
            HandleRaceNetControllerDespawned();

            if (_netController != null)
            {
                _netController.OnRaceNetControllerSpawned -= HandleRaceNetControllerSpawned;
                _netController.OnRaceNetControllerDespawned -= HandleRaceNetControllerDespawned;
            }
            FishNetSceneAdapter.OnSceneInitialized -= HandleSceneInitialized;

            OnRaceManagerDespawned?.Invoke();
        }
    }
}
