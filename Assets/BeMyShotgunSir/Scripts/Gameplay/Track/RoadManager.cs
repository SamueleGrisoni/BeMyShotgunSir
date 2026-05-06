using System;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public interface IRoadManager
    {
        void SetSeed(int seed);
        void SetRaceNetController(RaceNetController raceNetController);
        void SetDriver(Transform driver);
        void UpdateFirstPlayer(Transform playerTransform);
        void UpdateLastPlayer(Transform playerTransform);
    }

    public class RoadManager : NetworkBehaviour, IRoadManager
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<IRoadManager> OnRoadManagerSpawned;
        private int _seed = -1;
        private bool _isServer;
        private Transform _driver; //TODO change this to transform please
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
        public static event Action<List<GeneratedRoadChunkInfoWithItems>> OnSplitGeneratedProvided;

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

        public void SetSeed(int seed)
        {
            if (seed == -1)
                _seed = seed;
            _environmentSpawner.Init(_seed);
            TrackGenerator.OnSplitGenerated += (splitInfo) =>
            {
                Log.TLazy(() => "Receive split info from TrackGenerator, propagating", this);
                foreach (var info in splitInfo)
                {
                    Log.TLazy(() => "Receive info for " + info.roadChunkInfo.type, this);
                }
                OnSplitGeneratedProvided?.Invoke(splitInfo);
            };
            _trackGenerator.Init(_seed);
        }

        public void SetRaceNetController(RaceNetController raceNetController)
        {
            if (_raceNetController == null) _raceNetController = raceNetController;
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
            if (comp is StartFinishLineRoadChunk startFinish)
                _spawnPoints = startFinish.GridPositions;
            else
                Debug.Log("[Road Manager Server]: Failed to initialize spawn points, the first chunk is not a start finish line");
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
            Log.DLazy(() => $"RoadManager: Driver set to {driver.name}", this, _log);
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
                    SpawnRoadChunk();
                }
            }
            else
            {
                PooledRoadChunk lastChunk = _activeRoadChunks.Last.Value;
                Transform exitAnchor = lastChunk.Component.NextRoadAnchors[0];
                if (_firstPlayerTransform is null) return;
                if(_firstPlayerTransform.transform.position.z > exitAnchor.position.z - _serverBufferDistance)
                {
                    SpawnRoadChunk();
                }

                if(_lastPlayerTransform is null) return;
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
                int nextChunkIndex = chunkInfoWithItems.roadChunkInfo.index;
                PooledRoadChunk nextChunk =
                    chunkInfoWithItems.roadChunkInfo.type == RoadChunkType.TURN
                        ? _trackPooler.GetPooledRoadChunk(nextChunkIndex)
                        : _trackPooler.GetSpecialRoadChunk(nextChunkIndex);

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
    }
}
