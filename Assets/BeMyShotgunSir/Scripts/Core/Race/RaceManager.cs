using System;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
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
                Log.ELazy(() => "RaceManager: No RaceBinder found. Cannot bind race commands.", this);
                return;
            }
            if (_raceCommand == null)
            {
                Log.ELazy(() => "RaceManager: No RaceCommand found. Cannot bind race commands.", this);
                return;
            }
            if (_viewModel == null)
            {
                Log.ELazy(() => "RaceManager: No RaceViewModel found. Cannot bind race data.", this);
                return;
            }
            _binder.Bind(targets);
            _raceCommand.GetInitSnapshot_Request();
        }

        [SerializeField] private SORaceSounds _sounds;
        private SOAudioRequestEvent _audioRequestEvent;

        private void Awake()
        {
            Debug.Assert(_netController != null, "RaceManager: RaceNetController reference is not assigned in the inspector.", this);
            Debug.Assert(_sounds != null, "RaceManager: SORaceSounds reference is not assigned in the inspector.", this);
            _viewModel = new RaceViewModel();
            TryGetComponent(out _netController);
            _netController.OnRaceNetControllerSpawned += HandleRaceNetControllerSpawned;
            _netController.OnRaceNetControllerDespawned += HandleRaceNetControllerDespawned;
        }

        private void HandleRaceNetControllerDespawned() => throw new NotImplementedException();
        private void HandleRaceNetControllerSpawned() => throw new NotImplementedException();
    }
}
