using System;
using System.Collections;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public interface IRoadManager
    {
        void SetSeed(int? seed);
        void SetContext(RaceNetContext context);
        void SetDriver(Transform driver);
        void UpdateFirstPlayer(Transform playerTransform);
        void UpdateLastPlayer(Transform playerTransform);
    }

    public class RoadManager : NetworkBehaviour, IRoadManager
    {
        private bool _log = false;
        private bool _isInitialized = false;
        private bool _isSeedInitialized = false;
        public static event Action<IRoadManager> OnRoadManagerSpawned;
        private int? _seed = null;
        private bool _isServer;
        [SerializeField] private Transform _driver; //DEBUG serlializeField for debug purposes
        private Transform _firstPlayerTransform;
        private Transform _lastPlayerTransform;
        private List<Transform> _spawnPoints;
        private RaceManager _raceManager;
        private RaceNetController _raceNetController;
        private bool _started = false;

        [Header("Data References")]
        [SerializeField] private SOTrack _trackData;
        [Header("Generators References")]
        [Tooltip("Generator are responsible for generating the data of the track and items to spawn")]
        [SerializeField] private TrackGenerator _trackGenerator;
        [Header("Spawner References")]
        [Tooltip("Spawners are responsible for spawning the actual gameobjects in the scene")]
        [SerializeField] private RoadSpawner _roadSpawner;
        [SerializeField] private EnvironmentSpawner _environmentSpawner;
        [SerializeField] private ItemSpawner _itemSpawner;
        [Header("Other References")]
        [SerializeField] private TrackPooler _trackPooler;
        [SerializeField] private float _clientDespawnBufferDistance = 20f;
        [SerializeField] private float _serverBufferDistance = 150f;

        private LinkedList<PooledRoadChunk> _activeRoadChunks;
        private int _earlyCommitmentDirection = 0; // 0 means no early commitment set yet, -1 means early commitment to left, 1 means early commitment to right
        public static event Action<List<GeneratedRoadChunkInfoWithItems>> OnSplitGeneratedProvided;
        public static event Action<CrossroadSegmentInfo> OnCrossroadProvided;
        public static event Action<int> OnCommonGenerated;
        private bool _hasFinishLineSpawned = false;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TrackGenerator.OnSplitGenerated += PropagateOnSplitGenerated;
            TrackGenerator.OnCommonGenerated += PropagateCommonGenerated;
            TrackGenerator.OnCrossroadGenerated += PropagateOnCrossroad;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            OnRoadManagerSpawned?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsHostInitialized)
                return;
            OnRoadManagerSpawned?.Invoke(this);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            TrackGenerator.OnSplitGenerated -= PropagateOnSplitGenerated;
            TrackGenerator.OnCommonGenerated -= PropagateCommonGenerated;
            TrackGenerator.OnCrossroadGenerated -= PropagateOnCrossroad;
        }

        private void PropagateOnSplitGenerated(List<GeneratedRoadChunkInfoWithItems> splitInfo)
        {
            Log.DLazy(() => "Receive split info from TrackGenerator, propagating", this, _log);
            foreach (GeneratedRoadChunkInfoWithItems info in splitInfo)
            {
                Log.DLazy(() => "Receive info for " + info.roadChunkInfo.type, this, _log);
            }
            Log.DLazy(() => "Invoking OnSplitGeneratedProvided with split info count: " + splitInfo.Count, this);
            // OnSplitGeneratedProvided?.Invoke(splitInfo);
            StartCoroutine(NextFramePropagateSplit(splitInfo));
        }

        IEnumerator NextFramePropagateSplit(List<GeneratedRoadChunkInfoWithItems> splitInfo)
        {
            //DANGER: this is a workaround
            yield return null;
            OnSplitGeneratedProvided?.Invoke(splitInfo);
        }

        private void PropagateOnCrossroad(CrossroadSegmentInfo crossroadSegmentInfo)
        {
            Log.DLazy(() => "Invoking OnCrossroadProvided with info: " + crossroadSegmentInfo, this);
            OnCrossroadProvided?.Invoke(crossroadSegmentInfo);
        }

        private void PropagateCommonGenerated(int sectionLenght)
        {
            Log.DLazy(() => "Invoking OnCommonGenerated with section length: " + sectionLenght, this);
            OnCommonGenerated?.Invoke(sectionLenght);
        }

        public void SetSeed(int? seed)
        {
            if (_isSeedInitialized)
                return;
            if (seed is not int actualInt)
                return;
            _isSeedInitialized = true;
            _environmentSpawner.Init(actualInt);
            _trackGenerator.Init(actualInt);
            _seed = seed;
        }

        public void SetContext(RaceNetContext context)
        {
            //TODO
            // context.NetState.OnFinishLineChunkIdSet += qualcosa;
            // vedi tu se una certa iscrizione ti serve solo lato client o solo lato server o host
            //poi quando hai ottenuto il finish line chunk id puoi settarlo in autonomia nello store
            // context.NetState.SetFinishLineChunkId(finishlinechunkid);
            context.NetState.OnRaceTimerExpired += () => OnTimerRaceExpired(context);
            //context.NetState.OnFinishLineChunkIdSet += OnFinishLineChunkIdSet;
            if (_raceNetController == null) _raceNetController = context.NetController;
            _trackPooler.SetTrackData(_trackData);
            if (IsServerInitialized)
            {
                _isServer = true;
                InitSpawnPoints();
                _raceNetController.SetServerTrackReady_ServerRpc();
            }
            else { _raceNetController.SetTrackReady_ServerRpc(); }
        }

        [Server]
        private void InitSpawnPoints()
        {
            if (_activeRoadChunks == null) _activeRoadChunks = new LinkedList<PooledRoadChunk>();
            SpawnRoadChunk(true);
            RoadChunk comp = _activeRoadChunks.First.Value.Component;
            if (comp is StartLineRoadChunk startFinish)
                _spawnPoints = startFinish.GridPositions;
            else
                Log.WLazy(() => $"First road chunk is not a StartFinishLineRoadChunk, spawn points cannot be initialized properly.", this);
            PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
            RemoveChunk(oldRoadChunk);
            _activeRoadChunks.Clear();
            _raceNetController.SetSpawnPoints(_spawnPoints, Vector3.zero);
        }

        public void SetDriver(Transform driver)
        {
            if (_driver == null)
                _driver = driver;
            Vector3 pos = _driver.position;
            pos.y = 0.3f;
            _driver.position = pos;
            Log.DLazy(() => $"RoadManager: Driver set to {driver.name}", this);
            Initialize();
        }

        public void UpdateFirstPlayer(Transform playerTransform)
        {
            if (_isServer)
                _firstPlayerTransform = playerTransform;
        }

        public void UpdateLastPlayer(Transform playerTransform)
        {
            if (_isServer)
                _lastPlayerTransform = playerTransform;
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            if (_activeRoadChunks == null)
                _activeRoadChunks = new LinkedList<PooledRoadChunk>();

            for (int i = 0; i < _trackData.MaxActiveChunks; i++)
                SpawnRoadChunk();

            StartRace();
            _isInitialized = true;
        }

        public void StartRace()
        {
            _raceNetController.SetReadyToRace_ServerRpc();
            Log.DLazy(() => "Starting race.", this, _log);
            _started = true;
        }

        private void Update()
        {
            if (!_started)
                return;

            if (_driver == null || _activeRoadChunks.Count == 0)
                return;

            if (!_isServer)
            {
                PooledRoadChunk firstChunk = _activeRoadChunks.First.Value;
                Transform exitAnchor = firstChunk.Component.NextRoadAnchors[0];
                if (_driver.transform.position.z > exitAnchor.position.z + _clientDespawnBufferDistance)
                {
                    PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
                    RemoveChunk(oldRoadChunk);
                    if (!_hasFinishLineSpawned)
                    {
                        SpawnRoadChunk();
                    }
                }
            }
            else
            {
                PooledRoadChunk lastChunk = _activeRoadChunks.Last.Value;
                Transform exitAnchor = lastChunk.Component.NextRoadAnchors[0];
                if (_firstPlayerTransform is null) return;
                if (_firstPlayerTransform.transform.position.z > exitAnchor.position.z - _serverBufferDistance && !_hasFinishLineSpawned)
                {
                    SpawnRoadChunk();
                }

                if (_lastPlayerTransform is null) return;
                PooledRoadChunk firstChunk = _activeRoadChunks.First.Value;
                Transform firstChunkExitAnchor = firstChunk.Component.NextRoadAnchors[0];
                if (_lastPlayerTransform.transform.position.z >
                    firstChunkExitAnchor.position.z + _clientDespawnBufferDistance)
                {
                    PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
                    RemoveChunk(oldRoadChunk);
                }

            }
        }

        private void RemoveChunk(PooledRoadChunk chunk)
        {
            if (chunk.Component is ForkRoadChunk)
            {
                chunk.Component.GetComponent<ForkRoadChunk>().LeftBarrierObject.SetActive(false);
                chunk.Component.GetComponent<ForkRoadChunk>().RightBarrierObject.SetActive(false);
            }
            _activeRoadChunks.RemoveFirst();
            _environmentSpawner.ClearSpawnedProps(chunk.Component);
            _itemSpawner.ClearItemsFromChunk(chunk.Component);
            chunk.ReturnToPool();
        }

        private void SpawnRoadChunk(bool isPeakingStartFinishLine = false)
        {
            List<GeneratedRoadChunkInfoWithItems> generatedRoadChunkInfoList;
            if (!isPeakingStartFinishLine)
            {
                generatedRoadChunkInfoList =
                    _trackGenerator.GetGeneratedRoadChunkInfoWithItems();
            }
            else
            {
                generatedRoadChunkInfoList =
                    _trackGenerator.PeekStartFinishLine();
            }
            foreach (GeneratedRoadChunkInfoWithItems chunkInfoWithItems in
                generatedRoadChunkInfoList)
            {
                if (chunkInfoWithItems.roadChunkInfo.type == RoadChunkType.FINISH_LINE)
                {
                    Log.DLazy(() => "Spawning Final Line", this);
                    _hasFinishLineSpawned = true;
                }
                int nextChunkIndex = chunkInfoWithItems.roadChunkInfo.index;
                PooledRoadChunk nextChunk =
                    chunkInfoWithItems.roadChunkInfo.type == RoadChunkType.TURN
                        ? _trackPooler.GetPooledRoadChunk(nextChunkIndex)
                        : _trackPooler.GetSpecialRoadChunk(nextChunkIndex);

                nextChunk.Component.Type = chunkInfoWithItems.roadChunkInfo.type;
                nextChunk.Component.ChunkNumber = chunkInfoWithItems.roadChunkInfo.chunkNumber;

                _roadSpawner.PlaceRoadChunk(nextChunk, chunkInfoWithItems.roadChunkInfo.type,
                    chunkInfoWithItems.roadChunkInfo.position, _activeRoadChunks.Count);

                _environmentSpawner.PopulateChunk(nextChunk.Component);

                if (_isServer || chunkInfoWithItems.itemsToSpawn.TrueForAll(item => item.type == ItemType.OBSTACLE))
                {
                    _itemSpawner.PopulateChunkWithItems(nextChunk.Component, chunkInfoWithItems.itemsToSpawn);
                }

                _activeRoadChunks.AddLast(nextChunk);
            }
        }

        [Server]
        private void OnTimerRaceExpired(RaceNetContext context)
        {
            bool canGetFirstTeamProgress = context.NetState.TryGetTeamTrackProgress(context.NetState.Leaderboard[0], out TeamTrackProgress progress);
            if (!canGetFirstTeamProgress)
            {
                Log.WLazy(() => "Cannot get first team progress on timer expired, cannot set finish line chunk id", this);
                return;
            }

            RoadChunkType? firstTeamLastSpecialChunkType = progress.LastSpecialChunkType?.Type;
            if (firstTeamLastSpecialChunkType == null)
            {
                Log.WLazy(() => "First team last special chunk type is null on timer expired, defaulting to start line", this);
                firstTeamLastSpecialChunkType = RoadChunkType.START_LINE;
            }

            int finishLineId = _trackGenerator.ServerSetFinalSequence(firstTeamLastSpecialChunkType);

            Log.DLazy(() => $"Timer expired, set finish line chunk id to {finishLineId} based on first team last special chunk type {firstTeamLastSpecialChunkType}", this);

            RpcSetFinishLineChunkId(finishLineId);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void RpcSetFinishLineChunkId(int finishLineChunkId)
        {
            Log.WLazy(() => $"Finish line chunk id set to {finishLineChunkId}", this);
            _trackGenerator.SetFinishLineChunkId(finishLineChunkId);
        }
    }
}
