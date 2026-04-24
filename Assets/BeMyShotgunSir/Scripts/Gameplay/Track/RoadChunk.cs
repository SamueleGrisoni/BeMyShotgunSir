using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public enum RoadChunkType { STRAIGHT,TURN,STARTING_CROSSROAD, ENDING_CROSSROAD, START_FINISH_LINE}
    public class RoadChunk : MonoBehaviour
    {
        public virtual RoadChunkType Type { get; private set; } = RoadChunkType.TURN;
        [SerializeField] private Transform _spawnAnchor;
        public Transform SpawnAnchor => _spawnAnchor != null ? _spawnAnchor : transform;
        [field: SerializeField] public Transform[] NextRoadAnchors { get; private set; }
        [field: SerializeField] public PolygonSpawnArea[] PolygonSpawnArea { get; private set; }

        [field: SerializeField] public int TurnWeight { get; private set; }

        private int _indexInCurrentTrack;
        public void SetIndexInCurrentTrack(int index) => _indexInCurrentTrack = index;
    }
}
