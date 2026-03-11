using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class SpawnArea : MonoBehaviour
    {
        [field: SerializeField] public EnvChunkSize Size { get; private set; } = EnvChunkSize.Size1;
        [field: SerializeField] private Transform _spawnAnchor;
        public Transform SpawnAnchor => _spawnAnchor != null ? _spawnAnchor : transform;
        private Bounds _spawnBounds;

        private void OnValidate() => _spawnBounds = BoundsUtil.GetFullBounds(gameObject);

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_spawnBounds.center, _spawnBounds.size);
        }
    }
}
