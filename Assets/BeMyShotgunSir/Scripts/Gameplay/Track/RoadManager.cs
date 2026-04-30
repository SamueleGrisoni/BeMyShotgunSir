using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class RoadManager : MonoBehaviour
    {
        private bool _isInitialized = false;
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
        [SerializeField] private GameObject _driver;
        [SerializeField] private TrackPooler _trackPooler;
        [SerializeField] private float _despawnBufferDistance = 20f;

        private LinkedList<PooledRoadChunk> _activeRoadChunks;

        public void Init(int seed, bool isServer = false)
        {
            _environmentSpawner.Init(seed);
            _trackGenerator.Init(seed);
            Initialize(isServer);
        }

        private void Initialize(bool isServer = false)
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
