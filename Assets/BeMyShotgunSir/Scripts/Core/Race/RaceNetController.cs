using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public enum RaceRole
    {
        Driver,
        Shotgun
    }

    #region RaceNetController Data Structures
    public struct RacePlayerInitData
    {
        public int Seed;
        public bool IsHost;
        public NetworkObject Player;
        public int TeamId;
        public RaceRole Role;
        public RacePlayerInitData(int seed, bool isHost, NetworkObject player, int teamId, RaceRole role)
        {
            Seed = seed;
            IsHost = isHost;
            Player = null;
            TeamId = teamId;
            Role = role;
        }
        public RacePlayerInitData(RacePlayerInitData other, NetworkObject player)
        {
            Seed = other.Seed;
            IsHost = other.IsHost;
            Player = player;
            TeamId = other.TeamId;
            Role = other.Role;
        }
        public override string ToString() => $"Seed: {Seed}, IsHost: {IsHost}, Player: {Player.name}, teamId: {TeamId}, Role: {Role}";
    }

    public struct RacePlayerState
    {
        public int TeamId;
        public bool ReadyToRace;
        public RacePlayerState(int teamId, bool readyToRace)
        {
            TeamId = teamId;
            ReadyToRace = readyToRace;
        }
        public override string ToString() => $"TeamId: {TeamId}, ReadyToRace: {ReadyToRace}";
    }

    public struct InventoryData
    {
        public PowerUpType? PowerUp1;
        public PowerUpType? PowerUp2;
        public PowerUpType? PowerUp3;
        public PowerUpType? PowerUp4;
        public PowerUpType? PowerUp5;
        public int MaxPowerUps => 5;
        public PowerUpType? SelectedPowerUp;
        public bool IsFull => PowerUp1.HasValue && PowerUp2.HasValue && PowerUp3.HasValue && PowerUp4.HasValue && PowerUp5.HasValue;
        public bool IsEmpty => !PowerUp1.HasValue && !PowerUp2.HasValue && !PowerUp3.HasValue && !PowerUp4.HasValue && !PowerUp5.HasValue;
        public InventoryData(PowerUpType? powerUp1, PowerUpType? powerUp2, PowerUpType? powerUp3, PowerUpType? powerUp4, PowerUpType? powerUp5, PowerUpType? selectedPowerUp)
        {
            PowerUp1 = powerUp1;
            PowerUp2 = powerUp2;
            PowerUp3 = powerUp3;
            PowerUp4 = powerUp4;
            PowerUp5 = powerUp5;
            SelectedPowerUp = selectedPowerUp;
        }
        public override string ToString() => $"PowerUp1: {PowerUp1}, PowerUp2: {PowerUp2}, PowerUp3: {PowerUp3}, PowerUp4: {PowerUp4}, PowerUp5: {PowerUp5}, SelectedPowerUp: {SelectedPowerUp}";
    }

    public struct RaceTeamData
    {
        public int TeamId;
        public int DriverConnectionId;
        /// <summary>
        /// Indicate whether the driver is ready to receive the player prefab spawn call
        /// </summary>
        public RacePlayerInitData? DriverInitData;
        public int ShotgunConnectionId;
        public RacePlayerInitData? ShotgunInitData;
        public bool IsPlayerSpawned;
        public InventoryData? Inventory;
        public RaceTeamData(int teamId, int driverConnectionId, int shotgunConnectionId)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            ShotgunConnectionId = shotgunConnectionId;
            DriverInitData = null;
            ShotgunInitData = null;
            Inventory = null;
            IsPlayerSpawned = false;
        }

        public RaceTeamData(int teamId, int driverConnectionId, RacePlayerInitData? driverInitData, int shotgunConnectionId, RacePlayerInitData? shotgunInitData, bool isPlayerSpawned, InventoryData? inventory)
        {
            TeamId = teamId;
            DriverConnectionId = driverConnectionId;
            DriverInitData = driverInitData;
            ShotgunConnectionId = shotgunConnectionId;
            ShotgunInitData = shotgunInitData;
            Inventory = inventory;
            IsPlayerSpawned = isPlayerSpawned;
        }

        public RaceTeamData(RaceTeamData other, NetworkObject player)
        {
            TeamId = other.TeamId;
            DriverConnectionId = other.DriverConnectionId;
            if (other.DriverInitData.HasValue)
                DriverInitData = new RacePlayerInitData(other.DriverInitData.Value, player);
            else DriverInitData = null;
            ShotgunConnectionId = other.ShotgunConnectionId;
            if (other.ShotgunInitData.HasValue)
                ShotgunInitData = new RacePlayerInitData(other.ShotgunInitData.Value, player);
            else ShotgunInitData = null;
            IsPlayerSpawned = true;
            if (other.Inventory.HasValue)
                Inventory = other.Inventory;
            else Inventory = null;
        }

        public RaceTeamData(RaceTeamData other, InventoryData? inventory)
        {
            TeamId = other.TeamId;
            DriverConnectionId = other.DriverConnectionId;
            DriverInitData = other.DriverInitData;
            ShotgunConnectionId = other.ShotgunConnectionId;
            ShotgunInitData = other.ShotgunInitData;
            IsPlayerSpawned = other.IsPlayerSpawned;
            Inventory = inventory;
        }

        public override string ToString() => $"TeamId: {TeamId}, DriverConnectionId: {DriverConnectionId}, DriverInitData: {DriverInitData}, ShotgunConnectionId: {ShotgunConnectionId}, ShotgunInitData: {ShotgunInitData}, Inventory: {Inventory}, IsPlayerSpawned: {IsPlayerSpawned}";
    }
    #endregion

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
        #region local fields and properties
        private bool _log = true;
        private int _seed;
        private int _spawnedTeams = 0;
        private RaceTeamData[] _startingTeamsData;
        private ServerManager _serverManager;
        private IRaceManager_NetController _raceManager;
        private ILobbyNetController_RaceNetController _lobbyNetController;
        public static event Action<IRaceNetController> OnRaceNetControllerReady;
        public static event Action OnRaceNetControllerDespawned;
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private NetworkObject _playerPrefab;
        #endregion

        #region SyncVars
        private readonly Dictionary<int, RacePlayerState> _racePlayerStates = new Dictionary<int, RacePlayerState>();
        public IReadOnlyDictionary<int, RacePlayerState> PlayerStates => _racePlayerStates;
        private void OnRacePlayerStateChanged(SyncDictionaryOperation op, int key, RacePlayerState value, bool asServer)
        {
            if (asServer || _raceManager == null)
                return;
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                // _raceManager.SetRacePlayerStates_Response(op, key, value);
                case SyncDictionaryOperation.Set:
                    // _raceManager.SetRacePlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Remove:
                    // _raceManager.SetRacePlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Clear:
                    // _raceManager.SetRacePlayerStates_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
        }

        private readonly Dictionary<long, RaceTeamData> _raceTeamData = new Dictionary<long, RaceTeamData>();
        public IReadOnlyDictionary<long, RaceTeamData> TeamData => _raceTeamData;
        private void OnRaceTeamDataChanged(SyncDictionaryOperation op, long key, RaceTeamData value, bool asServer)
        {
            if (asServer || _raceManager == null) return;
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    // _raceManager.SetRaceTeamData_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Set:
                    // _raceManager.SetRaceTeamData_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Remove:
                    // _raceManager.SetRaceTeamData_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Clear:
                    // _raceManager.SetRaceTeamData_Response(op, key, value);
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    break;
            }
        }

        private readonly List<long> _leaderboard = new List<long>();
        public IReadOnlyList<long> Leaderboard => _leaderboard;
        private void OnLeaderboardChanged(SyncListOperation op, int index, long value, bool asServer)
        {
            if (asServer || _raceManager == null)
                return;
            switch (op)
            {
                case SyncListOperation.Add:
                    // _raceManager.SetLeaderboard_Response(op, index, value);
                    break;
                case SyncListOperation.Insert:
                    // _raceManager.SetLeaderboard_Response(op, index, value);
                    break;
                case SyncListOperation.Set:
                    // _raceManager.SetLeaderboard_Response(op, index, value);
                    break;
                case SyncListOperation.RemoveAt:
                    // _raceManager.SetLeaderboard_Response(op, index, value);
                    break;
                case SyncListOperation.Clear:
                    // _raceManager.SetLeaderboard_Response(op, index, value);
                    break;
                case SyncListOperation.Complete:
                    break;
                default:
                    break;
            }
        }
        #endregion

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
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (!IsServerInitialized)
                return;

            if (args.ConnectionState == RemoteConnectionState.Started)
                return;

            if (args.ConnectionState == RemoteConnectionState.Stopped)
                return;
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

        [Server]
        private void InitSyncValues()
        {
            foreach (KeyValuePair<int, LobbyPlayerState> lobbyPlayerState in _lobbyNetController.PlayerStates)
            {
                _racePlayerStates[lobbyPlayerState.Key] = new RacePlayerState
                (
                    teamId: lobbyPlayerState.Value.TeamId,
                    readyToRace: false
                );
            }
            foreach (KeyValuePair<int, LobbyTeamInfo> teamInfo in _lobbyNetController.TeamInfos)
            {
                _raceTeamData[teamInfo.Key] = new RaceTeamData
                (
                    teamId: teamInfo.Key,
                    driverConnectionId: teamInfo.Value.DriverConnectionId,
                    shotgunConnectionId: teamInfo.Value.ShotgunConnectionId
                );
            }
        }

        private void OnClientPresenceChangeStart(ClientPresenceChangeEventArgs args)
        {
            if (args.Scene.name != SceneName.Race.ToString())
                return;

            PreparePlayersInitData(args.Connection);
        }

        private void PreparePlayersInitData(NetworkConnection connection)
        {
            if (_racePlayerStates.TryGetValue(connection.ClientId, out RacePlayerState playerState))
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
                    spawnPoint = _spawnPoints[_spawnedTeams];
                    _spawnedTeams++;
                }


                if (_raceTeamData.TryGetValue(playerState.TeamId, out RaceTeamData teamData))
                {
                    if (teamData.DriverConnectionId == connection.ClientId)
                        _racePlayerStates[connection.ClientId] = new RacePlayerState(
                            teamId: playerState.TeamId,
                            readyToRace: true
                        );
                    else if (teamData.ShotgunConnectionId == connection.ClientId)
                        _racePlayerStates[connection.ClientId] = new RacePlayerState
                        (
                            teamId: playerState.TeamId,
                            readyToRace: true
                        );
                    else
                    {
                        Log.ELazy(() => $"Player with connection ID {connection.ClientId} is not assigned as driver or shotgun in their team. Cannot set them as ready.", this);
                        LogMessage_TargetRpc(connection, "Error setting ready state. Player not assigned as driver or shotgun in their team.", 1);
                        return;
                    }

                    RaceRole role = connection.ClientId == _raceTeamData[playerState.TeamId].DriverConnectionId ? RaceRole.Driver : RaceRole.Shotgun;
                    var initData = new RacePlayerInitData
                    (
                        seed: _seed,
                        isHost: IsHostInitialized,
                        player: null,
                        teamId: playerState.TeamId,
                        role: role
                    );

                    _raceTeamData[playerState.TeamId] = new RaceTeamData
                    (
                        teamId: teamData.TeamId,
                        driverConnectionId: teamData.DriverConnectionId,
                        driverInitData: role == RaceRole.Driver ? initData : _raceTeamData[playerState.TeamId].DriverInitData,
                        shotgunConnectionId: teamData.ShotgunConnectionId,
                        shotgunInitData: role == RaceRole.Shotgun ? initData : _raceTeamData[playerState.TeamId].ShotgunInitData,
                        isPlayerSpawned: false,
                        inventory: teamData.Inventory
                    );

                    if (_racePlayerStates[teamData.DriverConnectionId].ReadyToRace && _racePlayerStates[teamData.ShotgunConnectionId].ReadyToRace)
                    {
                        if (_raceTeamData[playerState.TeamId].IsPlayerSpawned)
                        {
                            Log.ELazy(() => $"Player for team ID {playerState.TeamId} is already spawned. Skipping spawn.", this);
                            LogMessage_TargetRpc(connection, "Player is already spawned. Skipping spawn.", 1);
                            return;
                        }
                        NetworkObject player = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
                        Spawn(player.gameObject, _lobbyNetController.PlayerStates[teamData.DriverConnectionId].Connection, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
                        _raceTeamData[playerState.TeamId] = new RaceTeamData(_raceTeamData[playerState.TeamId], player);
                        InitRace_TargetRpc(_lobbyNetController.PlayerStates[teamData.DriverConnectionId].Connection, _raceTeamData[playerState.TeamId].DriverInitData.Value);
                        InitRace_TargetRpc(_lobbyNetController.PlayerStates[teamData.ShotgunConnectionId].Connection, _raceTeamData[playerState.TeamId].ShotgunInitData.Value);

                    }
                }
                else
                {
                    Log.ELazy(() => $"Team data for team ID {playerState.TeamId} not found in RaceNetController. Cannot set player with connection ID {connection.ClientId} as ready.", this);
                    LogMessage_TargetRpc(connection, "Error setting ready state. Team data not found.", 1);
                }

            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReadyToRace_ServerRpc(bool ready, NetworkConnection connection = null)
        {
            if (_racePlayerStates.TryGetValue(connection.ClientId, out RacePlayerState playerState))
            {
                playerState.ReadyToRace = ready;
                _racePlayerStates[connection.ClientId] = playerState;
                CheckPlayersReadyToRace();
            }
            else
            {
                Log.ELazy(() => $"Player state for connection {connection.ClientId} not found in RaceNetController.", this);
                LogMessage_TargetRpc(connection, "Error setting ready state. Player state not found.", 1);
            }
        }

        [Server]
        private void CheckPlayersReadyToRace()
        {
            foreach (KeyValuePair<int, RacePlayerState> playerState in _racePlayerStates)
            {
                if (!playerState.Value.ReadyToRace)
                    return;
            }
            StartRace();
        }

        [Server]
        private void StartRace()
        {
            //TODO
        }

        [TargetRpc]
        private void InitRace_TargetRpc(NetworkConnection connection, RacePlayerInitData initData)
        {
            Log.DLazy(() => $"Initializing race for player {connection.ClientId} with init data: {initData}", this, _log);
            MovingDriver driver = initData.Player.GetComponentInChildren<MovingDriver>();
            if (driver != null)
            {
                //NOTE filter to avoid duplicating logic on the host, for which race server logic is sufficient
                if (IsHostInitialized)
                    _raceManager.InitRace_Response(_seed, true, driver.transform);
                else _raceManager.InitRace_Response(initData.Seed, false, driver.transform);
            }
            else
            {
                Log.ELazy(() => $"Player prefab {initData.Player.name} is missing a driver component. Cannot initialize race for this player.", this);
            }
        }
    }
}
