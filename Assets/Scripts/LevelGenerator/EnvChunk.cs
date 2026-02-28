using UnityEngine;

namespace BeMyShotgunSir.LevelGenerator
{
    [System.Serializable]
    public enum EnvChunkSize
    {
        Size1 = 1,
        Size2 = 2,
        Size3 = 3,
        MAX
    }
    public class EnvChunk : MonoBehaviour
    {
        [field: SerializeField] public EnvChunkSize Size { get; private set; } = EnvChunkSize.Size1;
        [SerializeField] private Transform _spawnAnchor;
        public Transform SpawnAnchor => _spawnAnchor != null ? _spawnAnchor : transform;
        [field: SerializeField] public SpawnArea[] RandomPropAreas { get; private set; }
        private int _indexInCurrentTrack;
        public void SetIndexInCurrentTrack(int index) => _indexInCurrentTrack = index;
    }
}
