using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.Pool;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
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

        private Dictionary<int, ObjectPool<PooledRoadChunk>> _roadChunkPools = null;
        private Dictionary<int, ObjectPool<PooledRoadChunk>> _specialRoadChunkPools = null;
        private Dictionary<int, ObjectPool<PooledEnvChunk>> _envChunkPools = null;
        private int[] _poolSizeByEnvSize = null;
        private Dictionary<int, ObjectPool<PooledRandomProp>> _randomPropPools = null;


        public void SetTrackData(SOTrack trackData)
        {
            if (_trackData != null)
                return; //no overwriting
            _trackData = trackData;
            InitPools(trackData);
        }

        public PooledRoadChunk GetPooledRoadChunk(int index) => _roadChunkPools[index].Get();
        public PooledRoadChunk GetSpecialRoadChunk(int index) => _specialRoadChunkPools[index].Get();
        public PooledEnvChunk GetPooledEnvChunk(int index) => _envChunkPools[index].Get();
        public PooledRandomProp GetPooledRandomProp(int index) => _randomPropPools[index].Get();

        public void ClearTrackData()
        {
            _trackData = null;
            _roadChunkPools = null;
            _specialRoadChunkPools = null;
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
            if (_trackData == null)
            {
                Debug.LogError("TrackPooler: No track data assigned. Pools cannot be initialized.");
                return;
            }

            _roadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(trackData.RoadChunks.Length);
            //Todo add start and finish crossroad and start line and finish line pools
            _specialRoadChunkPools = new Dictionary<int, ObjectPool<PooledRoadChunk>>(trackData.SpecialRoadChunks.Length);
            _envChunkPools = new Dictionary<int, ObjectPool<PooledEnvChunk>>(trackData.EnvChunks.Length);
            _randomPropPools = new Dictionary<int, ObjectPool<PooledRandomProp>>(trackData.RandomProps.Length);

            for (int i = 0; i < trackData.RoadChunks.Length; i++)
                SetupRoadPool(trackData, trackData.RoadChunks[i], i, _roadChunkPools);
            for (int i = 0; i < trackData.SpecialRoadChunks.Length; i++)
                SetupRoadPool(trackData, trackData.SpecialRoadChunks[i], i, _specialRoadChunkPools);

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
                actionOnRelease: obj => {
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
