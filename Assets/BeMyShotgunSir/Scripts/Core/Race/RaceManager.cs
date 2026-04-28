using System;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    [RequireComponent(typeof(RaceNetController))]
    public class RaceManager : NetworkBehaviour, IEventSender
    {
        public string SenderName => name;
        public static event Action<RaceManager> OnRaceManagerSpawned;
        public static event Action<RaceManager> OnRaceManagerDespawned;

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
            TryGetComponent(out _netController);
            _netController.OnRaceNetControllerSpawned += HandleRaceNetControllerSpawned;
            _netController.OnRaceNetControllerDespawned += HandleRaceNetControllerDespawned;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Log.DLazy(() => "RaceManager spawned on the network.", this);
        }

        private void HandleRaceNetControllerSpawned()
        {
            _raceCommand = new RaceCommand(_netController);
            _binder = new RaceBinder(_raceCommand, _viewModel);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            _viewModel.InitData();
            OnRaceManagerSpawned?.Invoke(this);
            //DANGER
            GameServices.Instance.SceneCoordinator.LoadRaceScene();
        }
        private void HandleRaceNetControllerDespawned()
        {
            _raceCommand = null;
            _binder = null;
            _audioRequestEvent = null;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            OnRaceManagerDespawned?.Invoke(this);
        }
    }
}
