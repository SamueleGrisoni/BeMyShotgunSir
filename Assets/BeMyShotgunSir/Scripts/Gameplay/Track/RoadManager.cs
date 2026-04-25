using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private TrackSeed _trackSeed;
        [SerializeField] private SOTrack _trackData;
        [SerializeField] private TrackManager _trackManager;
        [SerializeField] private EnvironmentManager _environmentManager;
        [SerializeField] private Transform _startingPoint;
        [SerializeField] private GameObject _driver;
        [SerializeField] private TrackPooler _trackPooler;
        [SerializeField] private float _despawnBufferDistance = 20f;

        private LinkedList<PooledRoadChunk> _activeRoadChunks;
        private Transform _lastPlacedNormalAnchor;
        private Transform _lastPlacedRightAnchor;

        private void Start()
        {
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
                var comp = _activeRoadChunks.First.Value.Component;
                if (comp is StartFinishLineRoadChunk startFinish)
                {
                    Vector3 startingPos = startFinish.GridPositions[_trackManager.GetRandomNumberInRange(0, 1)].position;
                    startingPos.y = 0.3f;
                    _driver.transform.position = startingPos;
                }
            }
            else
            {
                Debug.LogError("RoadManager: Driver reference is missing");
            }

        }

        private void Update()
        {
            if (_driver == null || _activeRoadChunks.Count == 0)
                return;

            //Remove chunk only when the ancor is passed (faster than computing the distance and works with turn (distance was eucledian))
            PooledRoadChunk firstChunk = _activeRoadChunks.First.Value;
            Transform exitAnchor = firstChunk.Component.NextRoadAnchors[0];
            if (_driver.transform.position.z > exitAnchor.position.z + _despawnBufferDistance)
            {
                PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
                _activeRoadChunks.RemoveFirst();
                _environmentManager.ClearSpawnedProps(oldRoadChunk.Component);
                oldRoadChunk.ReturnToPool();
                SpawnRoadChunk();
            }
        }

        private void SpawnRoadChunk()
        {
            List<GeneratedRoadChunkInfo> generatedRoadChunkInfoList =
                _trackManager.GetGeneratedRoadChunkInfo();
            foreach (GeneratedRoadChunkInfo generatedRoadChunk in generatedRoadChunkInfoList)
            {
                int nextChunkIndex = generatedRoadChunk.index;
                PooledRoadChunk nextChunk = generatedRoadChunk.type == RoadChunkType.TURN
                    ? _trackPooler.GetPooledRoadChunk(nextChunkIndex)
                    : _trackPooler.GetSpecialRoadChunk(nextChunkIndex);
                PlaceRoadChunk(nextChunk, generatedRoadChunk.type, generatedRoadChunk.position);
                _activeRoadChunks.AddLast(nextChunk);
            }
        }

        private void PlaceRoadChunk(PooledRoadChunk chunk, RoadChunkType type, RoadChunkPosition position)
        {
            switch (type)
            {
                case RoadChunkType.TURN:
                case RoadChunkType.STRAIGHT:
                case RoadChunkType.START_FINISH_LINE:
                    PlaceNormalRoadChunk(chunk, position);
                    break;
                case RoadChunkType.STARTING_CROSSROAD:
                    PlaceStartingCrossroad(chunk);
                    break;
                case RoadChunkType.ENDING_CROSSROAD:
                    PlaceEndingCrossroad(chunk);
                    break;
            }
            chunk.gameObject.SetActive(true);
            _environmentManager.PopulateChunk(chunk.Component);
        }

        private void PlaceNormalRoadChunk(PooledRoadChunk chunk, RoadChunkPosition position)
        {
            if (_activeRoadChunks.Count == 0)
            {
                if (position == RoadChunkPosition.MIDDLE)
                {
                    chunk.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    _lastPlacedNormalAnchor = chunk.Component.NextRoadAnchors[0];
                }
                else
                {
                    Debug.LogError($"activeRoadChunks is empty, can't place {position} road chunk");
                }
                return;
            }
            if (position == RoadChunkPosition.RIGHT)
            {
                if (_activeRoadChunks.Count == 1)
                {
                    Debug.LogError($"activeRoadChunks has only one element, can't place {position} road chunk. There must be at least a crossroad and a left chunk before placing a right chunk");
                }
                else
                {
                    AlignChunk(chunk, _lastPlacedRightAnchor);
                    _lastPlacedRightAnchor = chunk.Component.NextRoadAnchors[0];
                }
            }
            else
            {
                AlignChunk(chunk, _lastPlacedNormalAnchor);
                _lastPlacedNormalAnchor = chunk.Component.NextRoadAnchors[0];
            }
        }

        private void PlaceStartingCrossroad(PooledRoadChunk chunk)
        {
            AlignChunk(chunk, _lastPlacedNormalAnchor);
            _lastPlacedNormalAnchor = chunk.Component.NextRoadAnchors[0];
            _lastPlacedRightAnchor = chunk.Component.NextRoadAnchors[1];
        }

        private void PlaceEndingCrossroad(PooledRoadChunk chunk)
        {
            AlignChunk(chunk, _lastPlacedNormalAnchor);
            _lastPlacedNormalAnchor = chunk.Component.NextRoadAnchors[0];
            _lastPlacedRightAnchor = null;
        }

        private void AlignChunk(PooledRoadChunk chunk, Transform targetAnchor)
        {
            Transform entranceAnchor = chunk.Component.SpawnAnchor;
            chunk.transform.rotation = targetAnchor.rotation;
            Vector3 localOffset = chunk.transform.InverseTransformPoint(entranceAnchor.position);
            chunk.transform.position = targetAnchor.position - (chunk.transform.rotation * localOffset);
        }
    }
}
