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
        public bool IsInitialized { get; private set; } = false;

        public static event Action<ILobbyManager> OnLobbyManagerInitialized;
        public static event Action OnLobbyManagerDespawned;

        //binding
        private LobbyBinder _binder;
        private LobbyNetController _netController;
        private ILobbyBindTarget[] _bindTargets;
        private LobbyCommand _lobbyCommand;
        private LobbyClientProjector _projector;
        private LobbyNetStateStore _netState;
        public ILobbyNetStateRead NetState => _netController != null ? _netController.NetState : null;
        public LobbyViewModel ViewModel { get; private set; }
        LobbyCommand ILobbyInitialBindSource.Command => _lobbyCommand;
        LobbyViewModel ILobbyInitialBindSource.ViewModel => ViewModel;

        private void Awake()
        {
            TryGetComponent(out _netController);
            TryGetComponent(out _netState);
            TryGetComponent(out _projector);

            if (_netController == null || _netState == null || _projector == null)
                Log.ELazy(() => "LobbyManager requires LobbyNetController, LobbyNetStateStore and LobbyClientProjector on the same GameObject.", this);

            ViewModel = new LobbyViewModel(_netState);
            _projector.Init(ViewModel);
            _lobbyCommand = new LobbyCommand(_netController);
            _binder = new LobbyBinder(this);
        }

        private void OnEnable()
        {
            LobbySceneBootstrapper.OnLobbyBootStrapperAwakened += OnLobbyBootStrapperAwakened;
            RaceManager.OnRaceManagerSpawned += OnRaceManagerSpawned;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnLobbyManagerInitialized?.Invoke(this);
            Log.DLazy(() => "LobbyManager is initialized.", this, _log);

            if (IsServerInitialized)
                LoadLobbyScene();
        }

        private void OnLobbyBootStrapperAwakened(ILobbySceneBootstrapperInitializer initializer) => initializer.Initialize(this);

        [Server]
        private void LoadLobbyScene()
        {
            if (!IsServerInitialized)
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

        private void OnRaceManagerSpawned(IRaceManagerInitializer initializer) => initializer.Initialize(ViewModel, new LobbyNetContext(this, _netController, _netState, _projector));

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
            LobbySceneBootstrapper.OnLobbyBootStrapperAwakened -= OnLobbyBootStrapperAwakened;
            RaceManager.OnRaceManagerSpawned -= OnRaceManagerSpawned;
        }
    }
}
