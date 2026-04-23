using BeMyShotgunSir.Scripts.Events;
using FishNet.Object;
using UnityEngine;
using System;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Core.Audio;
using FishNet.Connection;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyManager : NetworkBehaviour, IEventSender
    {
        string IEventSender.SenderName => name;
        public static event Action<LobbyManager> OnLobbySpawned;
        public static event Action OnLobbyDespawned;

        private LobbyBinder _binder;
        private LobbyCommand _lobbyCommand;
        [SerializeField] private LobbyNetController _netController;
        [SerializeField] private SOLobbyData _data;
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
            if (_data == null)
            {
                Log.ELazy(() => "LobbyManager: No SOLobbyData found. Cannot bind lobby data.", this);
                return;
            }
            _binder.BindCommand(targets);
            _binder.BindData(targets);
        }

        [SerializeField] private SOLobbySounds _sounds;
        private SOAudioRequestEvent _audioRequestEvent;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Debug.Assert(_data != null, "LobbyManager: SOLobbyData reference is not assigned in the inspector.", this);
            _lobbyCommand = new LobbyCommand(this, _netController);
            _binder = new LobbyBinder(_lobbyCommand, _data);
            OnLobbySpawned?.Invoke(this);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            _data.InitData();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            OnLobbyDespawned?.Invoke();
        }

        public void AddPlayerToLobby(NetworkConnection conn) =>
        _netController.AddPlayerToLobby(conn);

        public void RemovePlayerFromLobby(NetworkConnection conn) =>
            _netController.RemovePlayerFromLobby(conn);

        public void AdjustPlayerCount(int delta) =>
            _netController.AdjustPlayerCount(delta);

        public void Refresh(ILobbyNetworkData data) => _data.Refresh(data);
        public void SetLobbyIP(string ip) => _data.SetLobbyIP(ip);
        public void SetPlayerCount(int prev, int next)
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
            _data.SetPlayerCount(next);
        }
        public void SetPlayerNames(string[] names) => _data.SetPlayerNames(names);

    }
}
