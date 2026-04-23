using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public struct PlayerLobbyState
    {
        public int ConnectionId;
        public string PlayerName;
        public bool IsReady;

        public PlayerLobbyState(NetworkConnection connection, string playerName)
        {
            ConnectionId = connection.ClientId;
            PlayerName = playerName;
            IsReady = false;
        }
    }

    public struct LobbyNetDataSnapshot : ILobbyNetData
    {
        public string LobbyIP { get; set; }
        public int PlayerCount { get; set; }
        public string[] PlayerNames { get; set; }

        public LobbyNetDataSnapshot(string lobbyIP, int playerCount, string[] playerNames)
        {
            LobbyIP = lobbyIP;
            PlayerCount = playerCount;
            PlayerNames = playerNames;
        }
    }
    public class LobbyNetController : NetworkBehaviour
    {
        private LobbyManager _lobbyManager;

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
            _playerStates.OnChange += OnPlayerStatesChanged;
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
            if (IsHostInitialized) return; //no request snapshot if host, we already have the data
            RequestNetDataSnapshot_ServerRpc();
        }

        /// <summary>
        /// When a client starts, it requests the current lobby data snapshot from the server to initialize its local lobby state. <br/>
        /// This ensures that late-joining clients have the correct lobby information upon joining.
        /// </summary>
        /// <param name="conn"></param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestNetDataSnapshot_ServerRpc(NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            var snapshot = new LobbyNetDataSnapshot(_lobbyIP.Value, _playerCount.Value, _playerNames.Value);

            InitNetData_TargetRpc(conn, snapshot);
        }

        /// <summary>
        /// Sends the current lobby data snapshot to the specified client connection. <br/>
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="snapshot"></param>
        [TargetRpc]
        private void InitNetData_TargetRpc(NetworkConnection conn, LobbyNetDataSnapshot snapshot)
        {
            if (conn == null)
                return;
            Log.DLazy(() => $"LobbyNetController: Executing TargetRpc for client '{conn.ClientId}: {snapshot.LobbyIP} : {snapshot.PlayerNames} : {snapshot.PlayerCount}'.", this);
            _lobbyManager.InitNetData(snapshot);
        }

        /// <summary>
        /// Adds a player to the lobby. This should be called on the server when a new client connects. <br/>
        /// </summary>
        /// <param name="conn"></param>
        [Server]
        public void AddPlayerToLobby(NetworkConnection conn) =>
            _playerCount.Value++;

        /// <summary>
        /// Removes a player from the lobby. This should be called on the server when a client disconnects. <br/>
        /// </summary>
        /// <param name="conn"></param>
        [Server]
        public void RemovePlayerFromLobby(NetworkConnection conn) => _playerCount.Value--;

        /// <summary>
        /// Adjusts the player count in the lobby. This should be called on the server when the player count needs to be updated. <br/>
        /// The delta parameter indicates how much to adjust the player count by (positive to add players, negative to remove players). <br/>
        /// <b>Important:</b> <br/>
        /// LobbyManager and LobbyNetController could still be null when a new connection is established, or when a connection is lost. This method allows to adjust the player count when they become available.
        /// </summary>
        /// <param name="delta"></param>
        [Server]
        public void AdjustPlayerCount(int delta)
        {
            int newValue = _playerCount.Value + delta;
            _playerCount.Value = Mathf.Max(0, newValue);
        }

        private readonly SyncDictionary<int, PlayerLobbyState> _playerStates = new();

        private void OnPlayerStatesChanged(SyncDictionaryOperation op, int key, PlayerLobbyState value, bool asServer)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                case SyncDictionaryOperation.Set:
                    _playerStates[key] = value;
                    break;
                case SyncDictionaryOperation.Remove:
                    _playerStates.Remove(key);
                    break;
                case SyncDictionaryOperation.Clear:
                    _playerStates.Clear();
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
        }

        private readonly SyncVar<string> _lobbyIP = new("Not connected");
        private void OnLobbyIPChanged(string prev, string next, bool asServer)
        {
            _lobbyManager.SetLobbyIP(next);
        }

        private readonly SyncVar<int> _playerCount = new(0);
        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            _lobbyManager.SetPlayerCount(prev, next);
        }

        private readonly SyncVar<string[]> _playerNames = new(new string[0]);

        private void OnPlayerNamesChanged(string[] prev, string[] next, bool asServer)
        {
            _lobbyManager.SetPlayerNames(next);
        }
    }
}
