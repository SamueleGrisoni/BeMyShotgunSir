using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeMyShotgunSir.Scripts.Core.Audio;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Gameplay.Players;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        void SetReadyToRace_ServerRpc(NetworkConnection connection = null);
    }

    public interface IRaceNetController_Manager : INetController_Manager
    {
        void InitRace(int seed);
    }

    public interface IRaceNetController : INetController, IRaceNetController_Command, IRaceNetController_Manager
    {
        void SetServerTrackReady_ServerRpc(NetworkConnection connection = null);
        void SetTrackReady_ServerRpc(NetworkConnection connection = null);
    }

    #endregion

    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetStateStore))]
    [RequireComponent(typeof(RaceClientProjector))]
    public class RaceNetController : NetController, IRaceNetController
    {
        //utility
        private bool _log = true;
        private bool _isInitialized = false;
        [SerializeField] private string _seed;

        //CONTEXT
        private LobbyNetStateStore _lobbyNetState;
        private RaceNetStateStore _netState;
        private PowerUpsNetController _powerUpsNetController;
        public IRaceNetStateRead NetState => _netState;
        private IRaceNetStateSubscribe _netStateSubscribe => _netState;
        private RaceClientProjector _clientProjector;

        //Specific
        private bool _isRaceStarted = false;
        private int _spawnedTeams = 0;
        private Vector3 _trackOrigin = Vector3.zero;
        private IRoadManager _roadManager;
        private List<Transform> _spawnPoints;
        private Dictionary<int, TeamProgress> _teamProgress;
        private float _leaderboardUpdateTimer = 0f;
        private float _raceTimer = 0f;
        private Queue<SpecialSegmentInfo> _specialSegmentInfoQueue = new Queue<SpecialSegmentInfo>();

        [Tooltip("Interval (seconds) between leaderboard updates on the server.")]
        [SerializeField] private float _tICK_INTERVAL = 0.2f;
        [SerializeField] private float _maxRaceTime = 30f;
        [SerializeField] private NetworkObject _roadManagerPrefab;
        [SerializeField] private TeamNetController _teamPrefab;
        [SerializeField] private SODriver _driverSO;
        private DriverController[] _driverPrefab;
        private int _driverPrefabIndex = 0;
        [SerializeField] private ShotgunController _shotgunPrefab;

        private void Awake()
        {
            TryGetComponent(out _netState);
            TryGetComponent(out _clientProjector);
            TryGetComponent(out _powerUpsNetController);

            if (_netState == null || _clientProjector == null || _powerUpsNetController == null)
                Log.ELazy(() => $"One or more required components are missing on RaceNetController.", this);

            if (_driverSO == null || _shotgunPrefab == null || _teamPrefab == null || _roadManagerPrefab == null)
                Log.ELazy(() => $"One or more required prefabs or scriptable objects are not assigned in the inspector of RaceNetController.", this);
            else _driverPrefab = _driverSO.DriverPrefabs;

            _teamProgress = new Dictionary<int, TeamProgress>();
            RoadManager.OnSpecialSegmentInfoProvided += OnSpecialSegmentInfoProvided;
            // TrackGenerator.OnFinishLineGenerated += OnFinishLineGenerated;
            //_specialSegmentInfoQueue.Enqueue(new SpecialSegmentInfo(RoadChunkType.START_LINE, 0)); //TODO rimosso perchè ora SpecialSegmentInfo viene inviato dopo: startline, startingCrossroad, endingCrossroad e FinishLine
        }

        private void OnSpecialSegmentInfoProvided(SpecialSegmentInfo info) => _specialSegmentInfoQueue.Enqueue(info); //TODO ask for finish line to be sent here as well

        public void OnEnable() =>
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnsubscribeEvents();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _netStateSubscribe.TeamTrackProgress_Sub.OnChange += OnTeamTrackProgressChanged;
        }

        [Server]
        private void OnTeamTrackProgressChanged(SyncDictionaryOperation op, int key, TeamTrackProgress value, bool asServer) //TODO test
        {
            // 1. Uscita immediata se non server o se non ci sono incroci da monitorare
            if (!asServer || _specialSegmentInfoQueue.Count == 0)
                return;

            _netState.TryGetTeamTrackProgress(key, out TeamTrackProgress currentProgress);
            _netState.TryGetTeamShotgunConnectionId(key, out int shotgunConnectionId);

            // 2. Aggiornamento progresso del player (Scanning della coda)
            for (int i = 0; i < _specialSegmentInfoQueue.Count; i++)
            {
                SpecialSegmentInfo specialChunk = _specialSegmentInfoQueue.ElementAt(i);
                if (currentProgress.CurrentChunkId == specialChunk.chunkNumber)
                {
                    int nextId = (_specialSegmentInfoQueue.Count > i + 1)
                        ? _specialSegmentInfoQueue.ElementAt(i + 1).chunkNumber
                        : currentProgress.NextSpecialChunkId;

                    var updated = new TeamTrackProgress(
                        currentProgress,
                        nextSpecialChunkId: nextId,
                        lastSpecialChunkType: new PortalInfo(specialChunk.chunkNumber, specialChunk.type),
                        isFinishLineNext: specialChunk.type == RoadChunkType.FINISH_LINE
                    );

                    if (_netState.FinishLineChunkId != -1 && _netState.FinishLineChunkId == currentProgress.CurrentChunkId)
                    {
                        _netState.TryGetDriverNob(key, out NetworkObject driverNob);
                        driverNob.GetComponent<DriverController>().ActivateControls_TargetRpc(_lobbyNetState.PlayerStates[shotgunConnectionId].Connection);
                        driverNob.GetComponent<DriverController>().ActivateControls_TargetRpc(_lobbyNetState.PlayerStates[key].Connection);
                        FinishRace_TargetRpc(_lobbyNetState.PlayerStates[shotgunConnectionId].Connection);
                        FinishRace_TargetRpc(_lobbyNetState.PlayerStates[key].Connection); //key is driver connection id
                    }

                    if (!updated.IsEqual(value))
                        _netState.SetTeamTrackProgress(key, updated);

                    break;
                }
            }

            // 3. Controllo se il team ha superato l'elemento in testa
            bool allPlayersSurpassedCrossroad = true;
            int headChunkId = _specialSegmentInfoQueue.Peek().chunkNumber;

            foreach (TeamTrackProgress progress in _netState.TeamTrackProgress.Values)
            {
                // Se un player non ha valore o il suo ID è <= a quello in testa, il team non ha ancora superato l'incrocio
                if (!progress.LastSpecialChunkType.HasValue || progress.LastSpecialChunkType.Value.Id <= headChunkId)
                {
                    allPlayersSurpassedCrossroad = false;
                    break;
                }
            }

            if (allPlayersSurpassedCrossroad)
            {
                if (_netState.FinishLineChunkId != -1 && _netState.FinishLineChunkId == headChunkId)
                {
                    Log.DLazy(() => $"All players surpassed the finish line! ChunkId: {headChunkId}.", this, _log);
                }
                SpecialSegmentInfo finishedCrossroad = _specialSegmentInfoQueue.Dequeue();
                _powerUpsNetController.DespawnSurpassedPowerUp(finishedCrossroad.chunkNumber);
                Log.DLazy(() => $"All players surpassed crossroad {finishedCrossroad.chunkNumber}. Dequeued.", this, _log);
            }
        }

        [TargetRpc]
        public void FinishRace_TargetRpc(NetworkConnection conn)
        {
            Log.DLazy(() => $"Team with connection ID {conn.ClientId} has finished the race! Showing finish screen.", this, _log);
            GameServices.Instance.Channels.AudioRequestEvent.RaiseEvent(null, new AudioRequest(RequestEnum.FinishRace), null);
            _clientProjector.ShowFinishScreen();
        } //TODO

        private void OnDisable() => UnsubscribeEvents();

        private void UnsubscribeEvents() =>
            RoadManager.OnRoadManagerSpawned -= OnRoadManagerSpawned;

        public void SetLobbyNetState(LobbyNetStateStore lobbyNetState) =>
            _lobbyNetState = lobbyNetState;

        [Server]
        public void InitRace(int seed = -1)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            int s = seed;
            if (string.IsNullOrEmpty(_seed))
            {
                if (seed == -1)
                    s = DateTime.Now.Ticks.ToString().GetHashCode();
            }
            else
            {
                s = _seed.GetHashCode();
            }
            _netState.SetSeed(s);

            Log.DLazy(() => $"Initializing race with seed {NetState.Seed}.", this);

            NetworkObject roadManager = Instantiate(_roadManagerPrefab);
            Spawn(roadManager.gameObject, null, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));
        }

        private void OnRoadManagerSpawned(IRoadManager manager)
        {
            if (_roadManager != null)
                return;
            _roadManager = manager;
            _roadManager.SetSeed(NetState.Seed);
            _roadManager.SetContext(new RaceNetContext(null, null, this, _netState, _clientProjector, null, null));
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
        private void TrySpawnTeams(NetworkConnection connection)
        {
            _netState.PrintRaceState(_log);
            if (!AreAllPlayersTrackReady())
                return;
            Log.DLazy(() => $"All players are ready with track. Attempting to spawn players.", this, _log);

            if (_spawnPoints == null || _spawnPoints.Count == 0)
            {
                Log.ELazy(() => "Spawn points not set in RaceNetController. Cannot spawn players.", this);
                return;
            }
            Transform spawnPoint = null;
            // Set the calling player as ready to race
            _netState.SetPlayerState(connection.ClientId, new RacePlayerState(NetState.PlayerStates[connection.ClientId], isReadyToRace: true));
            //Create a snapshot of team states to avoid issues with collection modification during iteration when setting teams as ready
            List<RaceTeamData> teamStatesSnapshot = new(NetState.TeamData.Values);
            foreach (RaceTeamData teamState in teamStatesSnapshot)
            {
                //get new spawn point for the team
                spawnPoint = _spawnPoints[_spawnedTeams];
                if (NetState.TryGetTeamData(teamState.TeamId, out RaceTeamData teamData))
                {
                    //spawn player when both driver and shotgun of the team are ready with track and player not spawned yet
                    if (!teamData.IsTeamSpawned
                    && NetState.PlayerStates[teamData.DriverConnectionId].IsTrackReady
                    && NetState.PlayerStates[teamData.ShotgunConnectionId].IsTrackReady)
                    {
                        TeamNetController team = Instantiate(_teamPrefab, spawnPoint.position, spawnPoint.rotation);
                        team.name = "Team " + teamState.TeamId;
                        team.SetTeamId(teamState.TeamId);
                        Spawn(team, null, UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName.Race.ToString()));

                        //MEMO I could move the setup logic into the team net controller changing ownership after spawn
                        //driver setup
                        DriverController player = Instantiate(_driverPrefab[_driverPrefabIndex], spawnPoint.position, spawnPoint.rotation);
                        player.name = "Driver Team " + teamState.TeamId + " Player " + teamData.DriverConnectionId;
                        player.NetworkObject.SetParent(team);
                        Spawn(player, _lobbyNetState.PlayerStates[teamData.DriverConnectionId].Connection);
                        _driverPrefabIndex = (_driverPrefabIndex + 1) % _driverPrefab.Length;

                        //Shotgun setup
                        ShotgunController shotgun = Instantiate(_shotgunPrefab, spawnPoint.position, spawnPoint.rotation);
                        shotgun.name = "Shotgun Team " + teamState.TeamId + " Player " + teamData.ShotgunConnectionId;
                        shotgun.NetworkObject.SetParent(player);
                        Spawn(shotgun, _lobbyNetState.PlayerStates[teamData.ShotgunConnectionId].Connection);

                        //MEMO this could be delegated to the team net controller (passing the reference to the server dictionary)
                        _teamProgress.Add(teamState.TeamId, new TeamProgress(player.GetComponentInChildren<MovementController>().transform, 0f));

                        _netState.SetTeamData(teamState.TeamId, new RaceTeamData(NetState.TeamData[teamState.TeamId], colorId: _spawnedTeams, teamNob: team.NetworkObject, driverNob: player.NetworkObject, shotgunNob: shotgun.NetworkObject, isTeamSpawned: true));
                        _spawnedTeams++;
                    }
                    else
                    {
                        Log.DLazy(() => $"Team {teamState.TeamId} is not ready to spawn. Driver track ready: {NetState.PlayerStates[teamData.DriverConnectionId].IsTrackReady}, Shotgun track ready: {NetState.PlayerStates[teamData.ShotgunConnectionId].IsTrackReady}, IsTeamSpawned: {NetState.TeamData[teamState.TeamId].IsTeamSpawned}", this, _log);
                    }
                }
                else
                {
                    Log.ELazy(() => $"Team data for team ID {teamState.TeamId} not found in RaceNetController. Cannot set player with connection ID {connection.ClientId} as ready.", this);
                    LogMessage_TargetRpc(connection, "Error setting ready state. Team data not found.", 2);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)] //TODO
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
        private void TryStartRace() //TODO
        {
            if (_isRaceStarted)
                return;
            if (NetState.AreAllPlayersReady())
            {
                _isRaceStarted = true;
                Countdown();

                Log.DLazy(() => $"All players are ready. Starting race.", this);
            }
        }

        private void Countdown()
        {
            ShowCountDown_ObserversRpc();
            // Start the race after the countdown
            StartCoroutine(StartRaceAfterCountdown());
        }

        [ObserversRpc]
        private void ShowCountDown_ObserversRpc()
        {
            GameServices.Instance.Channels.LoadingRequestEvent.RaiseEvent(null, false);
            GameServices.Instance.Channels.AudioRequestEvent.RaiseEvent(null, new AudioRequest(RequestEnum.Countdown), null);
            _clientProjector.ShowCountdown(true);
        }

        private IEnumerator StartRaceAfterCountdown()
        {
            yield return new WaitForSeconds(BMMSDefaults.COUNTDOWN_TIME); //DANGER
            StartRace_ObserversRpc();
        }

        [ObserversRpc]
        private void StartRace_ObserversRpc()
        {
            GameServices.Instance.Channels.AudioRequestEvent.RaiseEvent(null, new AudioRequest(RequestEnum.StartRace), null);
            _clientProjector.ShowCountdown(false);
            _roadManager.StartRace();

            foreach (RaceTeamData teamData in NetState.TeamData.Values)
            {
                if (teamData.DriverNob != null && teamData.ShotgunNob != null)
                {
                    teamData.DriverNob.TryGetComponent(out DriverController driver);
                    if (driver != null)
                        driver.ActivateControls_TargetRpc(_lobbyNetState.PlayerStates[teamData.DriverConnectionId].Connection);

                    teamData.ShotgunNob.TryGetComponent(out ShotgunController shotgun);
                    if (shotgun != null)
                        shotgun.ActivateControls_TargetRpc(_lobbyNetState.PlayerStates[teamData.ShotgunConnectionId].Connection);
                }
            }
        }

        private void Update()
        {
            if (!IsServerInitialized || !_isRaceStarted)
                return;

            UpdateTimer();
            UpdateLeaderboard();
        }

        [Server]
        private void UpdateTimer()
        {
            if (_netState.RaceTimerExpired)
                return;

            _raceTimer += Time.deltaTime;
            if (_raceTimer >= _maxRaceTime)
            {
                _netState.SetRaceTimerExpired();
                Log.DLazy(() => $"Race timer expired. RaceTimer: {_raceTimer}, MaxRaceTime: {_maxRaceTime}", this, _log);
            }
        }

        [Server]
        private void UpdateLeaderboard()
        {
            _leaderboardUpdateTimer += Time.deltaTime;
            if (_leaderboardUpdateTimer < _tICK_INTERVAL)
                return;
            _leaderboardUpdateTimer = 0f;

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
            Log.DLazy(() => "Updating leaderboard. Team order: " + string.Join(", ", sortedTeams), this);
            _netState.SetLeaderboard(sortedTeams);
            _roadManager.UpdateFirstPlayer(_teamProgress[sortedTeams[0]].transform);
            _roadManager.UpdateLastPlayer(_teamProgress[sortedTeams[sortedTeams.Count - 1]].transform);
        }
    }
}
