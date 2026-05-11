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
            InitPools(trackData);
        }

        public PooledRoadChunk GetPooledRoadChunk(int index) => _roadChunkPools[index].Get();
        public PooledRoadChunk GetSpecialRoadChunk(int index) => _specialRoadChunkPools[index].Get();

        private void Start()
        {
            if (_trackData != null)
                InitPools(_trackData);
        }

        private void InitPools(SOTrack trackData)
        {
            if (_trackData == null)
            {
                Debug.LogError("TrackPooler: No track data assigned. Pools cannot be initialized.");
                return;
            }

            _roadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(trackData.RoadChunks.Length);
            _specialRoadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(trackData.SpecialRoadChunks.Length);

            for (int i = 0; i < trackData.RoadChunks.Length; i++)
                SetupRoadPool(trackData, trackData.RoadChunks[i].gameObject, i, _roadChunkPools);
            for (int i = 0; i < trackData.SpecialRoadChunks.Length; i++)
            {
                SetupRoadPool(trackData, trackData.SpecialRoadChunks[i].gameObject, i, _specialRoadChunkPools);
            }
        }

        private void SetupRoadPool(SOTrack trackData, GameObject roadChunkPrefab, int index, Dictionary<int, ObjectPool<PooledRoadChunk>> dict)
        {
            dict[index] = new ObjectPool<PooledRoadChunk>(
                createFunc: () =>
                {
                    GameObject newRoadChunk = Instantiate(roadChunkPrefab, _roadChunksInactiveParent.transform);
                    PooledRoadChunk pooledRoadChunk = newRoadChunk.AddComponent<PooledRoadChunk>();
                    pooledRoadChunk.SetPool(dict[index]);
                    pooledRoadChunk.Component = newRoadChunk.GetComponent<RoadChunk>();
                    pooledRoadChunk.Component.SetIndexInCurrentTrack(index);
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
                defaultCapacity: trackData.RoadChunkInitPoolSize,
                maxSize: trackData.MaxPoolSize
            );

            // Pre-warm
            var temp = new PooledRoadChunk[trackData.RoadChunkInitPoolSize];
            for (int j = 0; j < trackData.RoadChunkInitPoolSize; j++) temp[j] = dict[index].Get();
            for (int j = 0; j < trackData.RoadChunkInitPoolSize; j++) dict[index].Release(temp[j]);
        }
    }
}
