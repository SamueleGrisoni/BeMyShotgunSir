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
        void SetDriver(GameObject driver);
        void UpdateFirstPlayer(Transform playerTransform);
    }

    public class RoadManager : NetworkBehaviour, IRoadManager
    {
        private bool _log = true;
        private bool _isInitialized = false;
        public static event Action<IRoadManager> OnRoadManagerSpawned;
        private int _seed = -1;
        private bool _isHostInitialized;
        private GameObject _driver; //TODO change this to transform please
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

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            OnRoadManagerSpawned?.Invoke(this);
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
                _raceNetController.ServerTrackReady();
            }
            else _raceNetController.TrackReady_ServerRpc();
        }

        [Server]
        private void InitSpawnPoints()
        {
            //driver spawnpoints for the moment
            _spawnPoints = new List<Transform>();
            for (int i = 0; i < 8; i++)
            {
                var spawnPointGO = new GameObject($"SpawnPoint_{i}");
                spawnPointGO.transform.position = Vector3.zero;
                _spawnPoints.Add(spawnPointGO.transform);
            }
            //TODO here we need to get the spawn points
        }

        public void SetDriver(GameObject driver)
        {
            if (driver == null)
                _driver = driver;
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

            if (_driver != null)
            {
                //todo this will be set by the server so the 2 sidecar do not overlap
                //(this implementation sucks ikik but I want to see the car on track tbh)
                RoadChunk comp = _activeRoadChunks.First.Value.Component;
                if (comp is StartFinishLineRoadChunk startFinish)
                {
                    Vector3 startingPos = startFinish
                        .GridPositions[_trackGenerator.GetRandomNumberInRange(0, 1)].position;
                    startingPos.y = 0.3f;
                    _driver.transform.position = startingPos;
                }
            }
            else
            {
                Debug.LogError("RoadManager: Driver reference is missing");
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
                _activeRoadChunks.RemoveFirst();
                _environmentSpawner.ClearSpawnedProps(oldRoadChunk.Component);
                oldRoadChunk.ReturnToPool();
                SpawnRoadChunk();
            }
        }

        private void SpawnRoadChunk()
        {
            List<GeneratedRoadChunkInfoWithItems> generatedRoadChunkInfoList =
                _trackGenerator.GetGeneratedRoadChunkInfoWithItems();
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
