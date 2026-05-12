using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class RoadSpawner : MonoBehaviour
    {
        private Transform _lastPlacedNormalAnchor;
        private Transform _lastPlacedRightAnchor;

        public void PlaceRoadChunk(PooledRoadChunk chunk, RoadChunkType type,
            RoadChunkPosition position, int activeRoadChunkCount)
        {
            switch (type)
            {
                case RoadChunkType.TURN:
                case RoadChunkType.STRAIGHT:
                case RoadChunkType.START_LINE:
                    PlaceNormalRoadChunk(chunk, position, activeRoadChunkCount);
                    break;
                case RoadChunkType.STARTING_CROSSROAD:
                    PlaceStartingCrossroad(chunk);
                    break;
                case RoadChunkType.ENDING_CROSSROAD:
                    PlaceEndingCrossroad(chunk);
                    break;
                default:
                    break;
            }
            chunk.gameObject.SetActive(true);
        }

        private void PlaceNormalRoadChunk(PooledRoadChunk chunk, RoadChunkPosition position, int activeRoadChunkCount)
        {
            if (activeRoadChunkCount == 0)
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
                if (activeRoadChunkCount == 1)
                {
                    Debug.LogError(
                        $"activeRoadChunks has only one element, can't place {position} road chunk. There must be at least a crossroad and a left chunk before placing a right chunk");
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
            chunk.transform.position =
                targetAnchor.position - (chunk.transform.rotation * localOffset);
        }
    }
}
