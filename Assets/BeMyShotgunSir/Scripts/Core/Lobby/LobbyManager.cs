using BeMyShotgunSir.Scripts.Events;
using FishNet.Object;
using UnityEngine;
using System;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Core.Audio;
using FishNet.Object.Synchronizing;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyManager : NetworkBehaviour, IEventSender
    {
        string IEventSender.SenderName => name;
        public static event Action<LobbyManager> OnLobbyManagerSpawned;
        public static event Action OnLobbyManagerDespawned;

        private LobbyBinder _binder;
        private LobbyCommand _lobbyCommand;
        [SerializeField] private LobbyNetController _netController;
        private LobbyViewModel _viewModel;
        public void BindLobby(ILobbyBindTarget[] targets)
        {
            if (_binder == null)
            {
                Log.ELazy(() => "LobbyManager: No LobbyBinder found. Cannot bind lobby commands.", this);
                return;
            }
            if (_lobbyCommand == null)
            {
                Log.ELazy(() => "LobbyManager: No LobbyCommand found. Cannot bind lobby commands.", this);
                return;
            }
            if (_viewModel == null)
            {
                Log.ELazy(() => "LobbyManager: No LobbyViewModel found. Cannot bind lobby data.", this);
                return;
            }
            _binder.Bind(targets);
            _lobbyCommand.GetInitSnapshot_Request();
        }

        [SerializeField] private SOLobbySounds _sounds;
        private SOAudioRequestEvent _audioRequestEvent;

        private void Awake()
        {
            Debug.Assert(_netController != null, "LobbyManager: LobbyNetController reference is not assigned in the inspector.", this);
            Debug.Assert(_sounds != null, "LobbyManager: SOLobbySounds reference is not assigned in the inspector.", this);
            _viewModel = new LobbyViewModel();
            TryGetComponent(out _netController);
            _netController.OnLobbyNetControllerSpawned += HandleLobbyNetControllerSpawned;
            _netController.OnLobbyNetControllerDespawned += HandleLobbyNetControllerDespawned;
        }

        private void HandleLobbyNetControllerSpawned()
        {
            _lobbyCommand = new LobbyCommand(_netController);
            _binder = new LobbyBinder(_lobbyCommand, _viewModel);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            _viewModel.InitData();
            OnLobbyManagerSpawned?.Invoke(this);
        }

        private void HandleLobbyNetControllerDespawned()
        {
            _lobbyCommand = null;
            _binder = null;
            _audioRequestEvent = null;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            OnLobbyManagerDespawned?.Invoke();
        }

        #region LobbyConnectionManagement
        /// <summary>
        /// Adjusts the player count in the lobby.<br/>
        /// <b>Important:</b> <br/>
        ///  Such a method is necessary because LobbyManager and LobbyNetController could still be null when a new connection is established, or when a connection is lost. This method allows to adjust the player count when they become available. <br/>
        /// </summary>
        /// <param name="delta"></param>
        public void AdjustPlayerCount(int delta) =>
            _netController.AdjustPlayerCount(delta);
        #endregion

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
        public void SetPlayerStates_Response(SyncDictionaryOperation op, int key, PlayerLobbyState value) => _viewModel.SetPlayerStates(op, key, value);

    }
}
