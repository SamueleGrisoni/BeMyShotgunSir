using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BeMyShotgunSir.LevelGenerator
{
    public class PooledRoadChunk : PooledObject<PooledRoadChunk, RoadChunk> { }
    public class PooledEnvChunk : PooledObject<PooledEnvChunk, EnvChunk> { }
    public class PooledRandomProp : PooledObject<PooledRandomProp, RandomProp> { }
    public class TrackPooler : MonoBehaviour
    {
        [SerializeField] private SOTrack _trackData;
        [SerializeField] private GameObject _roadChunksInactiveParent;
        [SerializeField] private GameObject _roadChunksActiveParent;
        [SerializeField] private GameObject _envChunksInactiveParent;
        [SerializeField] private GameObject _envChunksActiveParent;
        [SerializeField] private GameObject _randomPropsInactiveParent;
        [SerializeField] private GameObject _randomPropsActiveParent;

        [SerializeField] private Dictionary<int, ObjectPool<PooledRoadChunk>> _roadChunkPools = null;
        [SerializeField] private Dictionary<int, ObjectPool<PooledEnvChunk>> _envChunkPools = null;
        private int[] _poolSizeByEnvSize = null;
        [SerializeField] private Dictionary<int, ObjectPool<PooledRandomProp>> _randomPropPools = null;


        public void SetTrackData(SOTrack trackData)
        {
            if (_trackData != null)
                return; //no overwriting
            _trackData = trackData;
            InitPools(trackData);
        }

        public PooledRoadChunk GetPooledRoadChunk(int index) => _roadChunkPools[index].Get();
        public PooledEnvChunk GetPooledEnvChunk(int index) => _envChunkPools[index].Get();
        public PooledRandomProp GetPooledRandomProp(int index) => _randomPropPools[index].Get();

        public void ClearTrackData()
        {
            _trackData = null;
            _roadChunkPools = null;
            _envChunkPools = null;
            _randomPropPools = null;
        }

        private void Awake()
        {
            if (_trackData != null)
                InitPools(_trackData);
        }

        private void InitPools(SOTrack trackData)
        {
            if (_trackData == null) return;
            // Initialize pools for road chunks, env chunks, and random props
            _roadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(trackData.RoadChunks.Length);
            _envChunkPools = new Dictionary<int, ObjectPool<PooledEnvChunk>>(trackData.EnvChunks.Length);
            _randomPropPools = new Dictionary<int, ObjectPool<PooledRandomProp>>(trackData.RandomProps.Length);
            for (int i = 0; i < trackData.RoadChunks.Length; i++)
            {
                int index = i; //capture index for closure
                RoadChunkInfo chunk = trackData.RoadChunks[index];
                _roadChunkPools[index] = new ObjectPool<PooledRoadChunk>(
                    createFunc: () =>
                    {
                        GameObject newRoadChunk = Instantiate(chunk.RoadChunkPrefab, _roadChunksInactiveParent.transform);
                        PooledRoadChunk pooledRoadChunk = newRoadChunk.AddComponent<PooledRoadChunk>();
                        pooledRoadChunk.SetPool(_roadChunkPools[index]);
                        pooledRoadChunk.Component = newRoadChunk.GetComponent<RoadChunk>();
                        pooledRoadChunk.Component.SetIndexInCurrentTrack(index);
                        newRoadChunk.SetActive(false);
                        return newRoadChunk.GetComponent<PooledRoadChunk>();
                    },
                    actionOnRelease: obj =>
                    {
                        obj.gameObject.SetActive(false);
                        obj.transform.SetParent(_roadChunksInactiveParent.transform);
                    },
                    actionOnGet: obj =>
                    {
                        obj.transform.SetParent(_roadChunksActiveParent.transform);
                        //only universal resets here
                    },
                    actionOnDestroy: obj => Destroy(obj.gameObject),
                    collectionCheck: false,
                    defaultCapacity: trackData.RoadChunkInitPoolSize,
                    maxSize: trackData.MaxPoolSize
                );
                var prePooledRoadChunks = new PooledRoadChunk[trackData.RoadChunkInitPoolSize];
                for (int j = 0; j < trackData.RoadChunkInitPoolSize; j++)
                {
                    prePooledRoadChunks[j] = _roadChunkPools[index].Get();
                }
                for (int j = 0; j < trackData.RoadChunkInitPoolSize; j++)
                {
                    _roadChunkPools[index].Release(prePooledRoadChunks[j]);
                }
            }
            for (int h = 0; h < trackData.EnvChunks.Length; h++)
            {
                int index = h; //capture index for closure
                EnvChunkInfo chunk = trackData.EnvChunks[index];
                _envChunkPools[index] = new ObjectPool<PooledEnvChunk>(
                    createFunc: () =>
                    {
                        GameObject newEnvChunk = Instantiate(chunk.EnvChunkPrefab, _envChunksInactiveParent.transform);
                        PooledEnvChunk pooledEnvChunk = newEnvChunk.AddComponent<PooledEnvChunk>();
                        pooledEnvChunk.SetPool(_envChunkPools[index]);
                        pooledEnvChunk.Component = newEnvChunk.GetComponent<EnvChunk>();
                        pooledEnvChunk.Component.SetIndexInCurrentTrack(index);
                        newEnvChunk.SetActive(false);
                        return pooledEnvChunk;
                    },
                    actionOnRelease: obj =>
                    {
                        obj.gameObject.SetActive(false);
                        obj.transform.SetParent(_envChunksInactiveParent.transform);
                    },
                    actionOnGet: obj =>
                    {
                        obj.transform.SetParent(_envChunksActiveParent.transform);
                        //only universal resets here
                    },
                    actionOnDestroy: obj => Destroy(obj.gameObject),
                    collectionCheck: false,
                    defaultCapacity: trackData.EnvChunkInitPoolSize,
                    maxSize: trackData.MaxPoolSize
                );
                var prePooledEnvChunks = new PooledEnvChunk[trackData.EnvChunkInitPoolSize];
                for (int j = 0; j < trackData.EnvChunkInitPoolSize; j++)
                {
                    prePooledEnvChunks[j] = _envChunkPools[index].Get();
                }
                for (int j = 0; j < trackData.EnvChunkInitPoolSize; j++)
                {
                    _envChunkPools[index].Release(prePooledEnvChunks[j]);
                }
            }
            for (int i = 0; i < trackData.RandomProps.Length; i++)
            {
                int index = i; //capture index for closure
                GameObject randomProp = trackData.RandomProps[index];
                _randomPropPools[index] = new ObjectPool<PooledRandomProp>(
                    createFunc: () =>
                    {
                        GameObject newRandomProp = Instantiate(randomProp, _randomPropsInactiveParent.transform);
                        PooledRandomProp pooledRandomProp = newRandomProp.AddComponent<PooledRandomProp>();
                        pooledRandomProp.SetPool(_randomPropPools[index]);
                        pooledRandomProp.Component = newRandomProp.GetComponent<RandomProp>();
                        pooledRandomProp.Component.SetIndexInCurrentTrack(index);
                        newRandomProp.SetActive(false);
                        return pooledRandomProp;
                    },
                    actionOnRelease: obj =>
                    {
                        obj.gameObject.SetActive(false);
                        obj.transform.SetParent(_randomPropsInactiveParent.transform);
                    },
                    actionOnGet: obj =>
                    {
                        obj.transform.SetParent(_randomPropsActiveParent.transform);
                        //only universal resets here
                    },
                    actionOnDestroy: obj => Destroy(obj.gameObject),
                    collectionCheck: false,
                    defaultCapacity: trackData.RandomPropInitPoolSize,
                    maxSize: trackData.MaxPoolSize
                );
                var prePooledRandomProps = new PooledRandomProp[trackData.RandomPropInitPoolSize];
                for (int j = 0; j < trackData.RandomPropInitPoolSize; j++)
                {
                    prePooledRandomProps[j] = _randomPropPools[index].Get();
                }
                for (int j = 0; j < trackData.RandomPropInitPoolSize; j++)
                {
                    _randomPropPools[index].Release(prePooledRandomProps[j]);
                }
            }
        }
    }
}
