using BeMyShotgunSir.Scripts.Events;
using FishNet.Object;
using UnityEngine;
using System;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Core.Audio;
using FishNet.Object.Synchronizing;
using BeMyShotgunSir.Scripts.Core.Race;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    #region LobbyManagerInterfaces
    public interface ILobbyManager_Bootstrapper : IManager_Bootstrapper
    {
        /// <summary>
        /// Binds the LobbyCommand and LobbyViewModel to the given targets. <br/>
        /// </summary>
        /// <param name="targets"></param>
        void BindLobby(ILobbyBindTarget[] targets);
    }

    public interface ILobbyManager_NetController : IManager_NetController
    {
        void InitNetData_Response(ILobbyNetData data);
        void SetLobbyIP_Response(string ip);
        void SetLobbyInfo_Response(LobbyInfo info);
        void SetPlayerCount_Response(int prev, int next);
        void SetPlayerStates_Response(SyncDictionaryOperation op, int key, LobbyPlayerState value);
        void SetTeamInfos_Response(SyncDictionaryOperation op, long key, LobbyTeamInfo value);
    }

    public interface ILobbyManager : IManager, ILobbyManager_Bootstrapper, ILobbyManager_NetController { }

    #endregion

    [RequireComponent(typeof(LobbyNetController))]
    public class LobbyManager : NetworkBehaviour, ILobbyManager, ILobbyBindSources, IEventSender
    {
        private bool _log = true;
        string IEventSender.SenderName => name;


        /// <summary>
        /// Raised when a LobbyManager is spawned, regardless of whether it's ready to be used.
        /// </summary>
        public static event Action<ILobbyManager> OnLobbyManagerStarted;
        /// <summary>
        /// Raised when a LobbyManager is spawned and ready to be used, meaning the LobbyNetController is spawned and initialized, and the LobbyBinder and LobbyCommand are created. <br/>
        /// </summary>
        public static event Action<ILobbyManager> OnLobbyManagerReady;
        /// <summary>
        /// Raised when the LobbyManager is despawned. <br/>
        /// </summary>
        public static event Action OnLobbyManagerDespawned;
        private bool _isReady = false;
        private LobbyBinder _binder;
        private ILobbyBindTarget[] _bindTargets;
        private LobbyCommand _lobbyCommand;
        LobbyCommand ILobbyInitialBindSource.Command => _lobbyCommand;
        private LobbyViewModel _viewModel;
        LobbyViewModel ILobbyInitialBindSource.ViewModel => _viewModel;
        [SerializeField] private SOLobbySounds _sounds;
        private SOAudioRequestEvent _audioRequestEvent;

        public void BindLobby(ILobbyBindTarget[] targets)
        {
            _bindTargets = targets;
            if (_binder == null)
            {
                Log.ELazy(() => "No LobbyBinder found. Cannot bind lobby commands.", this);
                return;
            }
            if (_lobbyCommand == null)
            {
                Log.ELazy(() => "No LobbyCommand found. Cannot bind lobby commands.", this);
                return;
            }
            if (_viewModel == null)
            {
                Log.ELazy(() => "No LobbyViewModel found. Cannot bind lobby data.", this);
                return;
            }

            _binder.ExecuteInitialBind(targets);
            _lobbyCommand.GetInitSnapshot_Request();
        }

        private void Awake()
        {
            Debug.Assert(_sounds != null, "LobbyManager: SOLobbySounds reference is not assigned in the inspector.", this);
            _viewModel = new LobbyViewModel();
        }

        private void OnEnable()
        {
            LobbyNetController.OnLobbyNetControllerReady += OnLobbyNetControllerReady;
            LobbyNetController.OnLobbyNetControllerDespawned += OnLobbyNetControllerDespawned;
            FishNetSceneAdapter.OnSceneInitialized += OnSceneInitialized;
            RaceManager.OnRaceManagerStarted += OnRaceManagerStarted;
        }

        private void Start()
        {
            Log.DLazy(() => "LobbyManager started.", this, _log);
            OnLobbyManagerStarted?.Invoke(this);
        }

        private void OnLobbyNetControllerReady(ILobbyNetController netController)
        {
            if (_isReady)
                return;

            if (netController is ILobbyNetController_Command netController_Command)
                _lobbyCommand = new LobbyCommand(netController_Command);
            _binder = new LobbyBinder(this, this);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            _viewModel.InitData();

            _isReady = true;
            Log.DLazy(() => "LobbyManager is ready.", this, _log);
            OnLobbyManagerReady?.Invoke(this);

            if (!IsServerInitialized) //server instructions below
                return;

            GameServices.Instance.SceneCoordinator.LoadLobbyScene();
        }

        private void OnSceneInitialized(SceneName name)
        {
            if (name != SceneName.Lobby)
                return;

            if (_audioRequestEvent != null)
                _audioRequestEvent.RaiseEvent(this, new AudioRequest(_sounds.JoinLobbyClip, 1f).As2D(), null);
        }

        private void OnRaceManagerStarted(IRaceManager manager)
        {
            if (manager is not IRaceManager_LobbyManager manager_LobbyManager)
                return;
            manager_LobbyManager.InitViewModelData(_viewModel);
        }

        private void OnLobbyNetControllerDespawned()
        {
            if (!_isReady)
                return;

            _lobbyCommand = null;
            _binder = null;
            _audioRequestEvent = null;

            _isReady = false;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            OnLobbyNetControllerDespawned();
            UnsubscribeEvents();

            Log.DLazy(() => "LobbyManager despawned from the network.", this, _log);
            OnLobbyManagerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            LobbyNetController.OnLobbyNetControllerReady -= OnLobbyNetControllerReady;
            LobbyNetController.OnLobbyNetControllerDespawned -= OnLobbyNetControllerDespawned;
            FishNetSceneAdapter.OnSceneInitialized -= OnSceneInitialized;
            RaceManager.OnRaceManagerReady -= OnRaceManagerStarted;
        }

        public void InitNetData_Response(ILobbyNetData data) => _viewModel.InitNetData(data);
        public void SetLobbyIP_Response(string ip) => _viewModel.SetLobbyIP(ip);
        public void SetLobbyInfo_Response(LobbyInfo info) => _viewModel.SetLobbyInfo(info);
        public void SetPlayerCount_Response(int prev, int next)
        {
            if (prev < next)
            {
                if (_audioRequestEvent != null)
                    _audioRequestEvent.RaiseEvent(this, new AudioRequest(_sounds.JoinLobbyClip, 1f).As2D(), null);
            }
            if (prev > next)
            {
                if (_audioRequestEvent != null)
                    _audioRequestEvent.RaiseEvent(this, new AudioRequest(_sounds.LeaveLobbyClip, 1f).As2D(), null);
            }
            _viewModel.SetPlayerCount(next);
        }
        public void SetPlayerStates_Response(SyncDictionaryOperation op, int key, LobbyPlayerState value) => _viewModel.SetPlayerStates(op, key, value);

        public void SetTeamInfos_Response(SyncDictionaryOperation op, long key, LobbyTeamInfo value) => _viewModel.SetTeamInfos(op, key, value);
    }
}
