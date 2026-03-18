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
        [SerializeField] private SpawnArea[] _innerEnvChunkAreas;
        public SpawnArea[] InnerEnvChunkAreas => _innerEnvChunkAreas;
        [SerializeField] private SpawnArea[] _envChunkAreas;
        public SpawnArea[] EnvChunkAreas => _envChunkAreas;
        [SerializeField] private SpawnArea[] _innerRandomPropAreas;

        [field: SerializeField] public int TurnWeight { get; private set; }

        private int _indexInCurrentTrack;
        public void SetIndexInCurrentTrack(int index) => _indexInCurrentTrack = index;
    }
}
