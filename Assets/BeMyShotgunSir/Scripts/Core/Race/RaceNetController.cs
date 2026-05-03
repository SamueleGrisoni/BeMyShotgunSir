using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region Interfaces

    public interface IRaceNetController_Command : INetController_Command
    {
        void OnRefresh();
        void SetReadyToRace_ServerRpc(NetworkConnection connection = null);
    }

    public interface IRaceNetController_Manager : INetController_Manager
    {
        void InitRace(int seed);
    }

    public interface IRaceNetController : INetController, IRaceNetController_Command, IRaceNetController_Manager, IRaceNetState_Compact { }

    #endregion

    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetStateStore))]
    [RequireComponent(typeof(RaceClientProjector))]
    public class RaceNetController : NetController, IRaceNetController
    {
        //utility
        private bool _log = true;

        //references
        private bool _isInitialized = false;
        private int _spawnedTeams = 0;
        public ILobbyNetStateRead LobbyNetState;
        private IRaceNetStateStore_Mutator _netState;
        public IRaceNetStateRead NetState => _netState;
        private RaceClientProjector _clientProjector;
        private IRoadManager _roadManager;

        private List<Transform> _spawnPoints;
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private NetworkObject _roadManagerPrefab;

        private void Awake()
        {
            TryGetComponent(out _netState);
            TryGetComponent(out _clientProjector);

            if (_netState == null || _clientProjector == null)
                Log.ELazy(() => $"One or more required components are missing on RaceNetController.", this);
        }

        public void OnEnable()
        {
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents()
        {
            RoadManager.OnRoadManagerSpawned -= OnRoadManagerSpawned;
        }

        public void SetLobbyNetState(ILobbyNetStateRead lobbyNetState) =>
            LobbyNetState = lobbyNetState;

        public void OnRefresh()
        {
            if (IsController)
            {
                var snapshot = new RaceNetDataSnapshot(
                    playerStates: new Dictionary<int, RacePlayerState>(_netState.PlayerStates),
                    teamData: new Dictionary<int, RaceTeamData>(_netState.TeamData),
                    leaderboard: new List<int>(_netState.Leaderboard)
                );
                _clientProjector.InitNetData_Project(snapshot);
                return;
            }

            if (IsClientInitialized)
                RequestNetDataSnapshot_ServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestNetDataSnapshot_ServerRpc(NetworkConnection connection = null)
        {
            if (connection == null)
                return;

            var snapshot = new RaceNetDataSnapshot(
                playerStates: new Dictionary<int, RacePlayerState>(_netState.PlayerStates),
                teamData: new Dictionary<int, RaceTeamData>(_netState.TeamData),
                leaderboard: new List<int>(_netState.Leaderboard)
            );
            InitNetData_TargetRpc(connection, snapshot);
        }

        [TargetRpc]
        private void InitNetData_TargetRpc(NetworkConnection connection, RaceNetDataSnapshot snapshot)
        {
            if (connection == null)
                return;

            _clientProjector.InitNetData_Project(snapshot);
        }

        [Server]
        public void InitRace(int seed = -1)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            int s = seed;
            if (seed == -1)
                s = DateTime.Now.Ticks.ToString().GetHashCode();
            _netState.SetSeed(s);

            Log.DLazy(() => $"Initializing race with seed {_netState.GetSeed()}.", this, _log);

            NetworkObject roadManager = Instantiate(_roadManagerPrefab);
            Spawn(roadManager.gameObject, null, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
        }

        private void OnRoadManagerSpawned(IRoadManager manager)
        {
            if (_roadManager != null)
            {
                Log.WLazy(() => "Multiple RoadManagers detected. This is not expected. Ignoring additional instances.", this);
                return;
            }
            _roadManager = manager;
            _roadManager.SetSeed(_netState.GetSeed());
            _roadManager.SetRaceNetController(this, IsHostInitialized);
        }

        [Server]
        public void SetSpawnPoints(List<Transform> spawnPoints)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Log.WLazy(() => "Received null or empty spawn points list. Ignoring.", this);
                return;
            }
            _spawnPoints = spawnPoints;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ServerTrackReady(NetworkConnection connection = null)
        {
            if (connection == null || !IsHostInitialized)
                return;
            if (NetState.TryGetPlayerState(connection.ClientId, out RacePlayerState playerState))
                _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isTrackReady: true));
            else
                Log.ELazy(() => $"Player state for connection {connection.ClientId} not found in RaceNetController", this);

            TrySpawnPlayers(connection);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TrackReady_ServerRpc(NetworkConnection connection = null)
        {
            if (connection == null)
                return;
            if (NetState.TryGetPlayerState(connection.ClientId, out RacePlayerState playerState))
                _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isTrackReady: true));

            TrySpawnPlayers(connection);
        }

        [Server]
        private bool AreAllPlayersTrackReady()
        {
            foreach (RacePlayerState playerState in NetState.PlayerStates.Values)
            {
                if (!playerState.IsTrackReady)
                    return false;
            }
            return true;
        }



        [Server]
        private bool TrySpawnPlayers(NetworkConnection connection)
        {
            if (!AreAllPlayersTrackReady())
                return false;

            Transform spawnPoint;
            List<RacePlayerState> playerStatesSnapshot = new(NetState.PlayerStates.Values);
            foreach (RacePlayerState playerState in playerStatesSnapshot)
            {
                if (_spawnPoints == null || _spawnPoints.Count == 0)
                {
                    Log.ELazy(() => "Spawn points not set in RaceNetController. Cannot spawn players.", this);
                    return false;
                }
                spawnPoint = _spawnPoints[_spawnedTeams];

                if (NetState.TryGetTeamData(playerState.TeamId, out RaceTeamData teamData))
                {
                    if (teamData.DriverConnectionId == connection.ClientId)
                        _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isReadyToRace: true));
                    else if (teamData.ShotgunConnectionId == connection.ClientId)
                        _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isReadyToRace: true));
                    else
                    {
                        Log.ELazy(() => $"Player with connection ID {connection.ClientId} is not assigned as driver or shotgun in their team. Cannot set them as ready.", this);
                        LogMessage_TargetRpc(connection, "Error setting ready state. Player not assigned as driver or shotgun in their team.", 1);
                        return false;
                    }

                    RaceRole role = connection.ClientId == NetState.TeamData[playerState.TeamId].DriverConnectionId ? RaceRole.Driver : RaceRole.Shotgun;
                    _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, role: role));

                    if (NetState.PlayerStates[teamData.DriverConnectionId].IsTrackReady &&
                     NetState.PlayerStates[teamData.ShotgunConnectionId].IsTrackReady &&
                      !NetState.TeamData[playerState.TeamId].IsPlayerSpawned)
                    {
                        NetworkObject player = Instantiate(_playerPrefab, spawnPoint.position, spawnPoint.rotation);
                        Spawn(player.gameObject, LobbyNetState.PlayerStates[teamData.DriverConnectionId].Connection, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));

                        _netState.SetTeamData(playerState.TeamId, new RaceTeamData(NetState.TeamData[playerState.TeamId], player: player));

                        SetUpPlayer_TargetRpc(LobbyNetState.PlayerStates[teamData.DriverConnectionId].Connection, player, RaceRole.Driver);
                        SetUpPlayer_TargetRpc(LobbyNetState.PlayerStates[teamData.ShotgunConnectionId].Connection, player, RaceRole.Shotgun);

                        _netState.SetTeamData(playerState.TeamId, new RaceTeamData(NetState.TeamData[playerState.TeamId], isPlayerSpawned: true));

                        _spawnedTeams++;
                    }
                }
                else
                {
                    Log.ELazy(() => $"Team data for team ID {playerState.TeamId} not found in RaceNetController. Cannot set player with connection ID {connection.ClientId} as ready.", this);
                    LogMessage_TargetRpc(connection, "Error setting ready state. Team data not found.", 1);
                    return false;
                }

            }
            return true;
        }

        [TargetRpc]
        private void SetUpPlayer_TargetRpc(NetworkConnection connection, NetworkObject player, RaceRole role)
        {
            Log.DLazy(() => $"Setting up player {connection.ClientId} as {role}", this, _log);
            _clientProjector.SetUpPlayer_Response(player, role);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReadyToRace_ServerRpc(NetworkConnection connection = null)
        {
            if (NetState.TryGetPlayerState(connection.ClientId, out RacePlayerState playerState))
            {
                playerState.IsReadyToRace = true;
                _netState.SetPlayerState(connection.ClientId, playerState);
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
            if (NetState.AreAllPlayersReady())
                StartRace();
        }

        [Server]
        private void StartRace()
        {
            //TODO
        }



    }
}
