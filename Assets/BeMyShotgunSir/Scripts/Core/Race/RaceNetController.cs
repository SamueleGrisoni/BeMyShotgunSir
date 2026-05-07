using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;

// using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    #region Data Structures

    public class TeamProgress
    {
        public Transform transform;
        public float distance;
        public TeamProgress(Transform transform, float distance)
        {
            this.transform = transform;
            this.distance = distance;
        }
    }

    #endregion

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

    public interface IRaceNetController : INetController, IRaceNetController_Command, IRaceNetController_Manager { }

    #endregion

    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetStateStore))]
    [RequireComponent(typeof(RaceClientProjector))]
    public class RaceNetController : NetController, IRaceNetController
    {
        //utility
        private bool _log = true;

        private bool _isInitialized = false;
        private int _spawnedTeams = 0;
        public ILobbyNetStateRead LobbyNetState;
        private RaceNetStateStore _netState;
        public IRaceNetStateRead NetState => _netState;
        private RaceClientProjector _clientProjector;
        private IRoadManager _roadManager;

        private List<Transform> _spawnPoints;
        private bool _isRaceStarted = false;
        private Vector3 _trackOrigin = Vector3.zero;
        private Dictionary<int, TeamProgress> _teamProgress;

        [Header("Performance")]
        [Tooltip("Interval (seconds) between leaderboard updates on the server.")]
        [SerializeField] private float _tICK_INTERVAL = 0.2f;
        private float _leaderboardUpdateTimer = 0f;
        [SerializeField] private NetworkObject _roadManagerPrefab;
        [SerializeField] private NetworkObject _teamPrefab;
        [SerializeField] private NetworkObject _driverPrefab;
        [SerializeField] private NetworkObject _shotgunPrefab;

        private void Awake()
        {
            TryGetComponent(out _netState);
            TryGetComponent(out _clientProjector);

            if (_netState == null || _clientProjector == null)
                Log.ELazy(() => $"One or more required components are missing on RaceNetController.", this);

            _teamProgress = new Dictionary<int, TeamProgress>();
        }

        public void OnEnable() =>
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents() =>
            RoadManager.OnRoadManagerSpawned -= OnRoadManagerSpawned;

        public void SetLobbyNetState(ILobbyNetStateRead lobbyNetState) =>
            LobbyNetState = lobbyNetState;

        public void OnRefresh()
        {
            if (IsController)
            {
                var snapshot = new RaceNetDataSnapshot(
                    playerStates: new Dictionary<int, RacePlayerState>(NetState.PlayerStates),
                    teamData: new Dictionary<int, RaceTeamData>(NetState.TeamData),
                    leaderboard: new List<int>(NetState.Leaderboard)
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
                playerStates: new Dictionary<int, RacePlayerState>(NetState.PlayerStates),
                teamData: new Dictionary<int, RaceTeamData>(NetState.TeamData),
                leaderboard: new List<int>(NetState.Leaderboard)
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

            Log.DLazy(() => $"Initializing race with seed {NetState.GetSeed()}.", this, _log);

            NetworkObject roadManager = Instantiate(_roadManagerPrefab);
            Spawn(roadManager.gameObject, null, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
        }

        private void OnRoadManagerSpawned(IRoadManager manager)
        {
            if (_roadManager != null)
                return;
            _roadManager = manager;
            _roadManager.SetSeed(NetState.GetSeed());
            _roadManager.SetRaceNetController(this);
        }

        [Server]
        public void SetSpawnPoints(List<Transform> spawnPoints, Vector3 trackOrigin = default)
        {
            _spawnPoints = spawnPoints;
            _trackOrigin = trackOrigin;
        }


        [ServerRpc(RequireOwnership = false)]
        public void SetServerTrackReady_ServerRpc(NetworkConnection connection = null)
        {
            if (connection == null || !IsHostInitialized)
                return;
            if (NetState.TryGetPlayerState(connection.ClientId, out RacePlayerState playerState))
            {
                _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isTrackReady: true));
                Log.DLazy(() => $"PlayerHost with connection ID {connection.ClientId} is ready with track.", this, _log);
                TrySpawnTeams(connection);
            }
            else
                Log.ELazy(() => $"PlayerHost state for connection {connection.ClientId} not found in RaceNetController", this);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTrackReady_ServerRpc(NetworkConnection connection = null)
        {
            if (connection == null)
                return;
            if (NetState.TryGetPlayerState(connection.ClientId, out RacePlayerState playerState))
            {
                _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isTrackReady: true));
                Log.DLazy(() => $"Player with connection ID {connection.ClientId} is ready with track.", this, _log);
                TrySpawnTeams(connection);
            }
        }

        [Server]
        private bool AreAllPlayersTrackReady()
        {
            Log.DLazy(() => $"Checking if all players are ready with track.", this, _log);
            foreach (RacePlayerState playerState in NetState.PlayerStates.Values)
            {
                if (!playerState.IsTrackReady)
                    return false;
            }
            return true;
        }

        [Server]
        private bool TrySpawnTeams(NetworkConnection connection)
        {
            if (!AreAllPlayersTrackReady())
                return false;
            Log.DLazy(() => $"All players are ready with track. Attempting to spawn players.", this, _log);

            Transform spawnPoint = null;
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
                    // Set the calling player as ready to race (driver or shotgun of this team)
                    if (teamData.DriverConnectionId == connection.ClientId || teamData.ShotgunConnectionId == connection.ClientId)
                        _netState.SetPlayerState(connection.ClientId, new RacePlayerState(playerState, isReadyToRace: true));

                    //spawn player when both driver and shotgun of the team are ready with track and player not spawned yet
                    if (NetState.PlayerStates[teamData.DriverConnectionId].IsTrackReady &&
                     NetState.PlayerStates[teamData.ShotgunConnectionId].IsTrackReady &&
                      !NetState.TeamData[playerState.TeamId].IsPlayerSpawned)
                    {

                        NetworkObject team = Instantiate(_teamPrefab);
                        Spawn(team.gameObject, null, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));

                        NetworkObject player = Instantiate(_driverPrefab, spawnPoint.position, spawnPoint.rotation);
                        Spawn(player.gameObject, LobbyNetState.PlayerStates[teamData.DriverConnectionId].Connection, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
                        player.SetParent(team);

                        //Shotgun setup
                        NetworkObject shotgun = Instantiate(_shotgunPrefab);
                        // Ensure shotgun is a root object when spawned. Position it near the driver for visuals.
                        Spawn(shotgun.gameObject, LobbyNetState.PlayerStates[teamData.ShotgunConnectionId].Connection, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
                        // Set network parent after spawn so FishNet can move the root object between scenes.
                        shotgun.SetParent(player);


                        _teamProgress.Add(playerState.TeamId, new TeamProgress(player.GetComponentInChildren<MovingDriver>().transform, 0f));

                        _netState.SetTeamData(playerState.TeamId, new RaceTeamData(NetState.TeamData[playerState.TeamId], player: player));

                        SetUpTeam_TargetRpc(LobbyNetState.PlayerStates[teamData.DriverConnectionId].Connection, team, RaceRole.Driver);
                        SetUpTeam_TargetRpc(LobbyNetState.PlayerStates[teamData.ShotgunConnectionId].Connection, team, RaceRole.Shotgun);

                        _netState.SetTeamData(playerState.TeamId, new RaceTeamData(NetState.TeamData[playerState.TeamId], isPlayerSpawned: true));

                        _spawnedTeams++;
                        return true;
                    }
                    else
                    {
                        Log.DLazy(() => $"Team {playerState.TeamId} is not ready to spawn. Driver track ready: {NetState.PlayerStates[teamData.DriverConnectionId].IsTrackReady}, Shotgun track ready: {NetState.PlayerStates[teamData.ShotgunConnectionId].IsTrackReady}, IsPlayerSpawned: {NetState.TeamData[playerState.TeamId].IsPlayerSpawned}", this, _log);
                        return false;
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
        private void SetUpTeam_TargetRpc(NetworkConnection connection, NetworkObject team, RaceRole role)
        {
            Log.DLazy(() => $"Setting up team {connection.ClientId} as {role}", this, _log);
            _clientProjector.SetUpTeam_Response(connection.ClientId, team, role);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetReadyToRace_ServerRpc(NetworkConnection connection = null)
        {
            if (NetState.TryGetPlayerState(connection.ClientId, out RacePlayerState playerState))
            {
                playerState.IsReadyToRace = true;
                _netState.SetPlayerState(connection.ClientId, playerState);
                TryStartRace();
            }
            else
            {
                Log.ELazy(() => $"Player state for connection {connection.ClientId} not found in RaceNetController.", this);
                LogMessage_TargetRpc(connection, "Error setting ready state. Player state not found.", 1);
            }
        }

        [Server]
        private bool TryStartRace()
        {
            if (NetState.AreAllPlayersReady())
            {
                _isRaceStarted = true;
                Log.DLazy(() => $"All players are ready. Starting race.", this, _log);
                return true;
            }
            return false;
        }


        private void Update()
        {
            if (!IsHostInitialized || !_isRaceStarted)
                return;

            // Throttle leaderboard updates to reduce per-frame cost
            _leaderboardUpdateTimer += Time.deltaTime;
            if (_leaderboardUpdateTimer < _tICK_INTERVAL)
                return;
            _leaderboardUpdateTimer = 0f;

            UpdateLeaderboard();
        }

        [Server]
        private void UpdateLeaderboard()
        {
            if (_teamProgress.Count == 0)
                return;

            // Update distances for all teams (use forward progress on Z axis rather than euclidean distance to origin)
            foreach (KeyValuePair<int, TeamProgress> teamProgress in _teamProgress)
            {
                float dist = teamProgress.Value.transform.position.z - _trackOrigin.z;
                teamProgress.Value.distance = dist;
            }

            // Sort teams by distance (descending - higher distance is ahead)
            var sortedTeams = new List<int>(_teamProgress.Keys);
            sortedTeams.Sort((teamA, teamB) => _teamProgress[teamB].distance.CompareTo(_teamProgress[teamA].distance));

            // Update the networked leaderboard
            IReadOnlyList<int> lb = NetState.Leaderboard;
            if (lb.Count == sortedTeams.Count)
            {
                bool same = true;
                for (int i = 0; i < lb.Count; i++)
                {
                    if (lb[i] != sortedTeams[i]) { same = false; break; }
                }
                if (same) return;
            }
            _netState.SetLeaderboard(sortedTeams);
            _roadManager.UpdateFirstPlayer(_teamProgress[sortedTeams[0]].transform);
            _roadManager.UpdateLastPlayer(_teamProgress[sortedTeams[sortedTeams.Count - 1]].transform);
        }
    }
}
