using System;
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
        void SetSeed(int seed);
        void SetRaceNetController(RaceNetController raceNetController, bool isServer = false);
        void SetDriver(Transform driver);
        void UpdateFirstPlayer(Transform playerTransform);
    }

    public class RoadManager : NetworkBehaviour, IRoadManager
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<IRoadManager, bool> OnRoadManagerSpawned;
        private int _seed = -1;
        private bool _isHostInitialized;
        private Transform _driver; //TODO change this to transform please
        private Transform _firstPlayerTransform;
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
        [SerializeField] private float _despawnBufferDistance = 20f;

        private LinkedList<PooledRoadChunk> _activeRoadChunks;

        public override void OnStartServer()
        {
            base.OnStartServer();
            OnRoadManagerSpawned?.Invoke(this, true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsHostInitialized)
                return;
            OnRoadManagerSpawned?.Invoke(this, false);
        }

        public void SetSeed(int seed)
        {
            if (seed == -1)
                _seed = seed;
            _environmentSpawner.Init(_seed);
            _trackGenerator.Init(_seed);
        }

        public void SetRaceNetController(RaceNetController raceNetController, bool isHostInitialized = false)
        {
            if (_raceNetController == null) _raceNetController = raceNetController;
            if (isHostInitialized)
            {
                _isHostInitialized = true;
                InitSpawnPoints();
                _raceNetController.SetSpawnPoints(_spawnPoints);
                _raceNetController.SetServerTrackReady_ServerRpc();
            }
            else _raceNetController.SetTrackReady_ServerRpc();
        }

        [Server]
        private void InitSpawnPoints()
        {
            SpawnRoadChunk(true);
            RoadChunk comp = _activeRoadChunks.First.Value.Component;
            if (comp is StartFinishLineRoadChunk startFinish)
            {
                _spawnPoints = startFinish.GridPositions;
            }
            else
            {
                Debug.Log("[Road Manager Server]: Failed to initialize spawn points, the first chunk is not a start finish line");
            }
            PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
            RemoveChunk(oldRoadChunk);
            _activeRoadChunks.Clear();
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
            if (_isHostInitialized)
                _firstPlayerTransform = playerTransform;
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            _trackPooler.SetTrackData(_trackData);
            _activeRoadChunks = new LinkedList<PooledRoadChunk>();

            for (int i = 0; i < _trackData.MaxActiveChunks; i++)
            {
                SpawnRoadChunk();
            }

            StartRace();
            _isInitialized = true;
        }

        public void StartRace() => _started = true;

        private void Update()
        {
            if (!_started)
                return;

            if (_driver == null || _activeRoadChunks.Count == 0)
                return;

            //Remove chunk only when the ancor is passed (faster than computing the distance and works with turn (distance was eucledian))
            PooledRoadChunk firstChunk = _activeRoadChunks.First.Value;
            Transform exitAnchor = firstChunk.Component.NextRoadAnchors[0];
            if (_driver.transform.position.z > exitAnchor.position.z + _despawnBufferDistance)
            {
                PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
                RemoveChunk(oldRoadChunk);
                SpawnRoadChunk();
            }
        }

        private void RemoveChunk(PooledRoadChunk chunk)
        {
            //todo check, item are not removed?
            _activeRoadChunks.RemoveFirst();
            _environmentSpawner.ClearSpawnedProps(chunk.Component);
            chunk.ReturnToPool();
        }

        private void SpawnRoadChunk(bool isPeakingStartFinishLine=false)
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
                _itemSpawner.PopulateChunkWithItems(nextChunk.Component, chunkInfoWithItems.itemsToSpawn);

                _activeRoadChunks.AddLast(nextChunk);
            }
        }
    }
}
