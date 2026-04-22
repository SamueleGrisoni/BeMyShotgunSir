using BeMyShotgunSir.Scripts.Core.Audio;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.UI;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyManager : NetworkBehaviour, IEventSender, ILobbyData
    {
        string IEventSender.SenderName => name;
        public static event Action<LobbyManager> OnLobbySpawned;
        public static event Action OnLobbyDespawned;

        public LobbyMenu UiMenu { get; set; }
        [SerializeField] private SOLobbyData _data;
        [SerializeField] private AudioClip _joinLobbyClip;
        private SOAudioRequestEvent _audioRequestEvent;


        [Server]
        private void CleanUp()
        {
            _lobbyIP.OnChange -= OnLobbyIPChanged;
            _playerCount.OnChange -= OnPlayerCountChanged;
            _playerNames.OnChange -= OnPlayerNamesChanged;
        }

        public override void ClearReplicateCache() => base.ClearReplicateCache();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnLobbySpawned?.Invoke(this);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            Debug.Assert(_data != null, "LobbyManager: SOLobbyData reference is not assigned in the inspector.", this);
            _data.InitData();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyIP.OnChange += OnLobbyIPChanged;
            _playerCount.OnChange += OnPlayerCountChanged;
            _playerNames.OnChange += OnPlayerNamesChanged;

            _playerCount.Value = 0;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetLobbyIP(GameServices.Instance.NetworkManager.TransportManager.Transport.GetClientAddress() + ":" + GameServices.Instance.NetworkManager.TransportManager.Transport.GetPort());
            _data.Refresh(this);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            CleanUp();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            CleanUp();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            OnLobbyDespawned?.Invoke();
        }

        [Server]
        public void AddPlayerToLobby(NetworkConnection conn) =>
            // Qui in futuro potrai aggiungere il giocatore a una lista specifica
            // e leggerne i dati associati alla connessione
            _playerCount.Value++;

        [Server]
        public void RemovePlayerFromLobby(NetworkConnection conn) => _playerCount.Value--;


        private readonly SyncVar<string> _lobbyIP = new("Not connected");
        public string LobbyIP => _lobbyIP.Value;
        private void OnLobbyIPChanged(string prev, string next, bool asServer) => _data.SetLobbyIP(next);
        private readonly SyncVar<int> _playerCount = new(0);
        public int PlayerCount => _playerCount.Value;
        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (_audioRequestEvent != null)
                _audioRequestEvent.RaiseEvent(this, new AudioRequest(_joinLobbyClip, 1f).As2D(), null);

            _data.SetPlayerCount(next);
        }
        private readonly SyncVar<string[]> _playerNames = new(new string[0]);
        public string[] PlayerNames => _playerNames.Value;
        private void OnPlayerNamesChanged(string[] prev, string[] next, bool asServer) => _data.SetPlayerNames(next);

        [Server]
        public void SetLobbyIP(string newIP) =>
            _lobbyIP.Value = newIP;
    }
}
