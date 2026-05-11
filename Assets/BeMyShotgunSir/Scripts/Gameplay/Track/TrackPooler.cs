using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.Pool;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class PooledRoadChunk : PooledObject<PooledRoadChunk, RoadChunk> { }
    public class TrackPooler : MonoBehaviour
    {
        private SOTrack _trackData;
        [SerializeField] private GameObject _roadChunksInactiveParent;
        [SerializeField] private GameObject _roadChunksActiveParent;
        private Dictionary<int, ObjectPool<PooledRoadChunk>> _roadChunkPools = null;
        private Dictionary<int, ObjectPool<PooledRoadChunk>> _specialRoadChunkPools = null;

        public void SetTrackData(SOTrack trackData)
        {
            _trackData = trackData;
            InitPools();
        }

        public PooledRoadChunk GetPooledRoadChunk(int index) => _roadChunkPools[index].Get();
        public PooledRoadChunk GetSpecialRoadChunk(int index) => _specialRoadChunkPools[index].Get();

        private void Start()
        {
            if (_trackData != null)
                InitPools();
        }

        private void InitPools()
        {
            if (_trackData == null)
            {
                Debug.LogError("TrackPooler: No track data assigned. Pools cannot be initialized.");
                return;
            }

            _roadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(_trackData.RoadChunks.Length);
            _specialRoadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(_trackData.SpecialRoadChunks.Length);

            for (int i = 0; i < _trackData.RoadChunks.Length; i++)
                SetupRoadPool(_trackData.RoadChunks[i].gameObject, i, _roadChunkPools);
            for (int i = 0; i < _trackData.SpecialRoadChunks.Length; i++)
            {
                SetupRoadPool(_trackData.SpecialRoadChunks[i].gameObject, i, _specialRoadChunkPools);
            }
        }

        private void SetupRoadPool(GameObject roadChunkPrefab, int index, Dictionary<int, ObjectPool<PooledRoadChunk>> dict)
        {
            dict[index] = new ObjectPool<PooledRoadChunk>(
                createFunc: () =>
                {
                    GameObject newRoadChunk = Instantiate(roadChunkPrefab, _roadChunksInactiveParent.transform);
                    PooledRoadChunk pooledRoadChunk = newRoadChunk.AddComponent<PooledRoadChunk>();
                    pooledRoadChunk.SetPool(dict[index]);
                    pooledRoadChunk.Component = newRoadChunk.GetComponent<RoadChunk>();
                    newRoadChunk.SetActive(false);
                    return pooledRoadChunk;
                },
                actionOnRelease: obj =>
                {
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(_roadChunksInactiveParent.transform);
                },
                actionOnGet: obj => obj.transform.SetParent(_roadChunksActiveParent.transform),
                actionOnDestroy: obj => Destroy(obj.gameObject),
                collectionCheck: false,
                defaultCapacity: _trackData.RoadChunkInitPoolSize,
                maxSize: _trackData.MaxPoolSize
            );

            // Pre-warm
            var temp = new PooledRoadChunk[_trackData.RoadChunkInitPoolSize];
            for (int j = 0; j < _trackData.RoadChunkInitPoolSize; j++) temp[j] = dict[index].Get();
            for (int j = 0; j < _trackData.RoadChunkInitPoolSize; j++) dict[index].Release(temp[j]);
        }
    }
}
