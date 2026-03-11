using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class RoadChunk : MonoBehaviour
    {
        [SerializeField] private Transform _spawnAnchor;
        public Transform SpawnAnchor => _spawnAnchor != null ? _spawnAnchor : transform;
        [field: SerializeField] public Transform[] NextRoadAnchors { get; private set; }
        [SerializeField] private SpawnArea[] _innerEnvChunkAreas;
        public SpawnArea[] InnerEnvChunkAreas => _innerEnvChunkAreas;
        [SerializeField] private SpawnArea[] _envChunkAreas;
        public SpawnArea[] EnvChunkAreas => _envChunkAreas;
        [SerializeField] private SpawnArea[] _innerRandomPropAreas;

        private int _indexInCurrentTrack;
        public void SetIndexInCurrentTrack(int index) => _indexInCurrentTrack = index;
    }
}
