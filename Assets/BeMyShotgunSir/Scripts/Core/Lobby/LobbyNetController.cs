using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyNetController : NetworkBehaviour, ILobbyNetworkData
    {
        private LobbyManager _lobbyManager;
        public string LobbyIP => _lobbyIP.Value;
        public int PlayerCount => _playerCount.Value;
        public string[] PlayerNames => _playerNames.Value;

        private void Awake()
        {
            if (!TryGetComponent(out _lobbyManager))
                Debug.LogError("LobbyNetController: LobbyManager not found as parent!", this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _lobbyIP.OnChange += OnLobbyIPChanged;
            _playerCount.OnChange += OnPlayerCountChanged;
            _playerNames.OnChange += OnPlayerNamesChanged;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _playerCount.Value = 0;
            _lobbyIP.Value = GameServices.Instance.NetworkManager.TransportManager.Transport.GetClientAddress() + ":" + GameServices.Instance.NetworkManager.TransportManager.Transport.GetPort();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _lobbyIP.OnChange -= OnLobbyIPChanged;
            _playerCount.OnChange -= OnPlayerCountChanged;
            _playerNames.OnChange -= OnPlayerNamesChanged;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _lobbyManager.Refresh(this);
        }

        private readonly SyncVar<string> _lobbyIP = new("Not connected");
        private void OnLobbyIPChanged(string prev, string next, bool asServer)
        {
            if (prev != next)
                _lobbyManager.SetLobbyIP(next);
        }

        private readonly SyncVar<int> _playerCount = new(0);
        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (prev != next)
                _lobbyManager.SetPlayerCount(prev, next);
        }

        private readonly SyncVar<string[]> _playerNames = new(new string[0]);

        private void OnPlayerNamesChanged(string[] prev, string[] next, bool asServer)
        {
            if (prev != next)
                _lobbyManager.SetPlayerNames(next);
        }

        [Server]
        public void AddPlayerToLobby(NetworkConnection conn) =>
            _playerCount.Value++;

        [Server]
        public void RemovePlayerFromLobby(NetworkConnection conn) => _playerCount.Value--;

        [Server]
        public void AdjustPlayerCount(int delta)
        {
            int newValue = _playerCount.Value + delta;
            _playerCount.Value = Mathf.Max(0, newValue);
        }
    }
}
