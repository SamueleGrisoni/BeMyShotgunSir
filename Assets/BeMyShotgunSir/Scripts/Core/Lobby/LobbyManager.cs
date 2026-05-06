using BeMyShotgunSir.Scripts.Events;
using FishNet.Object;
using UnityEngine;
using System;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Core.Race;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    #region Interfaces
    public interface ILobbyManager_Bootstrapper : IManager_Bootstrapper
    {
        /// <summary>
        /// Binds the LobbyCommand and LobbyViewModel to the given targets. <br/>
        /// </summary>
        /// <param name="targets"></param>
        void BindLobby_Initial(ILobbyBindTarget[] targets);
    }
    public interface ILobbyManager : IManager, ILobbyManager_Bootstrapper { }

    #endregion
    [RequireComponent(typeof(LobbyNetStateStore))]
    [RequireComponent(typeof(LobbyNetController))]
    [RequireComponent(typeof(LobbyClientProjector))]
    public class LobbyManager : NetworkBehaviour, ILobbyManager, ILobbyBindSources, IEventSender
    {
        //utility
        private bool _log = true;
        string IEventSender.SenderName => name;
        public bool IsReady { get; private set; } = false;

        public static event Action<ILobbyManager> OnLobbyManagerStarted;
        public static event Action<ILobbyManager> OnLobbyManagerInitialized;
        public static event Action OnLobbyManagerDespawned;

        //binding
        private LobbyBinder _binder;
        private LobbyNetController _lobbyNetController;
        private ILobbyBindTarget[] _bindTargets;
        private LobbyCommand _lobbyCommand;
        private LobbyClientProjector _projector;
        private LobbyNetStateStore _netState;
        public ILobbyNetStateRead NetState => _lobbyNetController != null ? _lobbyNetController.NetState : null;
        public LobbyViewModel ViewModel { get; private set; }
        LobbyCommand ILobbyInitialBindSource.Command => _lobbyCommand;
        LobbyViewModel ILobbyInitialBindSource.ViewModel => ViewModel;

        private void Awake()
        {
            TryGetComponent(out _lobbyNetController);
            TryGetComponent(out _netState);
            TryGetComponent(out _projector);

            if (_lobbyNetController == null || _netState == null || _projector == null)
                Log.ELazy(() => "LobbyManager requires LobbyNetController, LobbyNetStateStore and LobbyClientProjector on the same GameObject.", this);

            ViewModel = new LobbyViewModel();
            ViewModel.InitData();
            _projector.Init(ViewModel);
            _lobbyCommand = new LobbyCommand(_lobbyNetController);
            _binder = new LobbyBinder(this);
        }

        private void OnEnable()
        {
            RaceManager.OnRaceManagerSpawned += OnRaceManagerSpawned;
            if (_lobbyNetController != null)
                _lobbyNetController.OnReady += TryAnnounceReady;
            FishNetSceneAdapter.OnSceneInitialized += OnSceneInitialized;
        }

        private void OnRaceManagerSpawned(IRaceManagerInitializer initializer) => initializer.Initialize(ViewModel, _netState);

        private void Start()
        {
            Log.DLazy(() => "LobbyManager started.", this, _log);
            OnLobbyManagerStarted?.Invoke(this);
        }

        private void TryAnnounceReady()
        {
            if (IsReady)
                return;

            IsReady = true;
            OnLobbyManagerInitialized?.Invoke(this);
            Log.DLazy(() => "LobbyManager is ready.", this, _log);

            LoadLobbyScene();
        }

        [Server]
        private void LoadLobbyScene()
        {
            if (!IsServerInitialized) //server instructions below
                return;

            GameServices.Instance.SceneCoordinator.LoadLobbyScene();
        }


        public void BindLobby_Initial(ILobbyBindTarget[] targets)
        {
            if (_binder == null || _lobbyCommand == null || ViewModel == null)
            {
                Log.WLazy(() => $"LobbyManager cannot bind lobby components. {(_binder == null ? "LobbyBinder" : _lobbyCommand == null ? "LobbyCommand" : "LobbyViewModel")} is null", this);
                return;
            }
            _bindTargets = targets;
            _binder.ExecuteInitialBind(targets);
        }

        private void OnSceneInitialized(SceneName name)
        {
            if (name != SceneName.Lobby)
                return;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();

            Log.DLazy(() => "LobbyManager despawned from the network.", this, _log);
            OnLobbyManagerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            if (_lobbyNetController != null)
                _lobbyNetController.OnReady -= TryAnnounceReady;
            FishNetSceneAdapter.OnSceneInitialized -= OnSceneInitialized;
        }
    }
}
