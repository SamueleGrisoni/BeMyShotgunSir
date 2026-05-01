using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region RaceNetController Interfaces
    public interface IRaceNetController_Command : INetController_Command { }
    public interface IRaceNetController_Manager : INetController_Manager
    {
        void InitRace(RoadManager roadManager);
    }
    public interface IRaceNetController_LobbyNetController
    {
        void SetLobbyNetController(ILobbyNetController_RaceNetController lobbyNetController);
    }
    public interface IRaceNetController : INetController, IRaceNetController_Command, IRaceNetController_Manager, IRaceNetController_LobbyNetController { }
    #endregion

    [RequireComponent(typeof(IRaceManager))]
    public class RaceNetController : NetController, IRaceNetController
    {
        private bool _log = true;
        private int _seed;
        private ServerManager _serverManager;
        private IRaceManager_NetController _raceManager;
        private ILobbyNetController_RaceNetController _lobbyNetController;
        public static event Action<IRaceNetController> OnRaceNetControllerReady;
        public static event Action OnRaceNetControllerDespawned;
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private NetworkObject _playerPrefab;



        private void OnEnable() =>
            RaceManager.OnRaceManagerStarted += OnRaceManagerStarted;

        private void OnRaceManagerStarted(IRaceManager manager)
        {
            if (manager is not IRaceManager_NetController manager_NetController)
                return;

            if (_raceManager != null)
            {
                Log.ELazy(() => "RaceManager reference is already set. Multiple RaceManagers are not supported.", this);
                return;
            }

            _raceManager = manager_NetController;

            Log.DLazy(() => "RaceNetController is ready.", this, _log);
            OnRaceNetControllerReady?.Invoke(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _serverManager = GameServices.Instance.NetworkManager.ServerManager;
            _serverManager.OnRemoteConnectionState += HandleRemoteConnectionState;
            FishNet.InstanceFinder.SceneManager.OnClientPresenceChangeStart += OnClientPresenceChangeStart;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            //NOTE host operations must be written below this check, otherwise the host will execute them twice (once as server, once as client)
            if (!IsServerInitialized)
                return;
        }


        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
            Log.DLazy(() => "RaceNetController despawned from the network.", this, _log);
            OnRaceNetControllerDespawned?.Invoke();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            if (_serverManager != null)
                _serverManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
        }

        [Server]
        public void SetLobbyNetController(ILobbyNetController_RaceNetController lobbyNetController)
        {
            if (_lobbyNetController != null)
            {
                Log.ELazy(() => "LobbyNetController reference is already set in RaceNetController.", this);
                return;
            }
            _lobbyNetController = lobbyNetController;
        }

        [Server]
        public void InitRace(RoadManager roadManager)
        {
            //MEMO: this gets called before OnClientPresenceChangeStart

            _seed = DateTime.Now.Ticks.ToString().GetHashCode();
            // TODO: waiting interfaces from @SamueleGrisoni here
            // var playerSpanws = _roadManager.GetStartingPositions();
            InitSyncValues();
        }

        private void OnClientPresenceChangeStart(ClientPresenceChangeEventArgs args)
        {
            if (args.Scene.name != SceneName.Race.ToString())
                return;

            if (_lobbyNetController.PlayerStates.TryGetValue(args.Connection.ClientId, out LobbyPlayerState playerState))
            {
                Transform spawnPoint;
                if (_spawnPoints.Count == 0)
                {
                    Log.ELazy(() => "No spawn points assigned to RaceNetController. Spawning player at origin.", this);
                    spawnPoint = new GameObject("DefaultSpawnPoint").transform;
                    spawnPoint.position = Vector3.zero + Vector3.up * 1f;
                }
                else
                {
                    spawnPoint = _spawnPoints[playerState.ConnectionId];
                }
                NetworkObject player = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
                Spawn(player.gameObject, args.Connection, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
                InitRace_TargetRpc(args.Connection, _seed, player);
            }
        }


        [TargetRpc]
        private void InitRace_TargetRpc(NetworkConnection connection, int seed, NetworkObject player)
        {
            MovingDriver driver = player.GetComponentInChildren<MovingDriver>();
            if (driver != null)
            {
                //NOTE filter to avoid duplicating logic on the host, for which race server logic is sufficient
                if (IsHostInitialized)
                    _raceManager.InitRace_Response(_seed, true, driver.transform);
                else _raceManager.InitRace_Response(seed, false, driver.transform);
            }
            else
            {
                Log.ELazy(() => $"Player prefab {player.name} is missing a driver component. Cannot initialize race for this player.", this);
            }
        }

        private void InitSyncValues() { }

        [Server]
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (!IsServerInitialized)
                return;

            if (args.ConnectionState == RemoteConnectionState.Started)
                return;

            if (args.ConnectionState == RemoteConnectionState.Stopped)
                return;
        }
    }
}
