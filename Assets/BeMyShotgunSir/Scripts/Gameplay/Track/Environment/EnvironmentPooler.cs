using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine.Pool;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class PooledCityProps : PooledObject<PooledCityProps, CityProps> { }
    public class PooledBuilding : PooledObject<PooledBuilding, Building> { }
    public class EnvironmentPooler : MonoBehaviour
    {
        private SOEnvironment _environmentData;

        [SerializeField] private GameObject _propsInactiveParent;
        [SerializeField] private GameObject _propsActiveParent;
        [SerializeField] private GameObject _bigBuildingsInactiveParent;
        [SerializeField] private GameObject _bigBuildingsActiveParent;
        [SerializeField] private GameObject _mediumBuildingsInactiveParent;
        [SerializeField] private GameObject _mediumBuildingsActiveParent;
        [SerializeField] private GameObject _smallBuildingsInactiveParent;
        [SerializeField] private GameObject _smallBuildingsActiveParent;

        private Dictionary<int, ObjectPool<PooledCityProps>> _propsPools = null;
        private Dictionary<int, ObjectPool<PooledBuilding>> _bigBuildingPools = null;
        private Dictionary<int, ObjectPool<PooledBuilding>> _mediumBuildingPools = null;
        private Dictionary<int, ObjectPool<PooledBuilding>> _smallBuildingPools = null;

        public PooledCityProps GetPropsPrefab(int index) => _propsPools[index].Get();
        public PooledBuilding GetBigBuildingPooled(int index) => _bigBuildingPools[index].Get();
        public PooledBuilding GetMediumBuildingPooled(int index) => _mediumBuildingPools[index].Get();
        public PooledBuilding GetSmallBuildingPooled(int index) => _smallBuildingPools[index].Get();

        public void SetEnvironmentData(SOEnvironment environmentData)
        {
            _environmentData = environmentData;
            InitPools(environmentData);
        }

        private void Start()
        {
            if (_environmentData != null)
                InitPools(_environmentData);
        }

        private void InitPools(SOEnvironment environmentData)
        {
            if (_environmentData == null)
            {
                Debug.LogError("EnvironmentPooler: No environment data assigned. Pools cannot be initialized.");
                return;
            }

            _propsPools = new Dictionary<int, ObjectPool<PooledCityProps>>(environmentData.CityProps.Length);
            _bigBuildingPools = new Dictionary<int, ObjectPool<PooledBuilding>>(environmentData.BigBuildings.Length);
            _mediumBuildingPools = new Dictionary<int, ObjectPool<PooledBuilding>>(environmentData.MediumBuildings.Length);
            _smallBuildingPools = new Dictionary<int, ObjectPool<PooledBuilding>>(environmentData.SmallBuildings.Length);

            for (int i = 0; i < environmentData.CityProps.Length; i++)
            {
                SetupCityPropsPool(environmentData.CityProps[i].gameObject, i, _propsPools);
            }
            for (int i = 0; i < environmentData.BigBuildings.Length; i++)
            {
                SetupBuildingPool(environmentData.BigBuildings[i].gameObject, i, _bigBuildingPools);
            }
            for (int i = 0; i < environmentData.MediumBuildings.Length; i++)
            {
                SetupBuildingPool(environmentData.MediumBuildings[i].gameObject, i, _mediumBuildingPools);
            }
            for (int i = 0; i < environmentData.SmallBuildings.Length; i++)
            {
                SetupBuildingPool(environmentData.SmallBuildings[i].gameObject, i, _smallBuildingPools);
            }
        }

        private void SetupCityPropsPool(GameObject prefab, int index, Dictionary<int, ObjectPool<PooledCityProps>> dict)
        {
            dict[index] = new ObjectPool<PooledCityProps>(
                createFunc: () =>
                {
                    GameObject newObj = Instantiate(prefab, _propsInactiveParent.transform);
                    PooledCityProps pooledObj = newObj.AddComponent<PooledCityProps>();
                    pooledObj.SetPool(dict[index]);
                    pooledObj.Component = newObj.GetComponent<CityProps>();
                    newObj.SetActive(false);
                    return pooledObj;
                },
                actionOnRelease: obj =>
                {
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(_propsInactiveParent.transform);
                },
                actionOnGet: obj =>
                {
                    obj.gameObject.SetActive(true);
                    obj.transform.SetParent(_propsActiveParent.transform);
                });
            var temp = new PooledCityProps[_environmentData.PropsInitPoolSize];
            for (int i = 0; i < _environmentData.PropsInitPoolSize; i++) temp[i] = dict[index].Get();
            for (int i = 0; i < _environmentData.PropsInitPoolSize; i++) dict[index].Release(temp[i]);
        }

        private void SetupBuildingPool(GameObject prefab, int index,
            Dictionary<int, ObjectPool<PooledBuilding>> dict)
        {
            dict[index] = new ObjectPool<PooledBuilding>(
                createFunc: () =>
                {
                    GameObject newObj = Instantiate(prefab, _bigBuildingsInactiveParent.transform);
                    PooledBuilding pooledObj = newObj.AddComponent<PooledBuilding>();
                    pooledObj.SetPool(dict[index]);
                    pooledObj.Component = newObj.GetComponent<Building>();
                    newObj.SetActive(false);
                    return pooledObj;
                },
                actionOnRelease: obj =>
                {
                    obj.gameObject.SetActive(false);
                    obj.transform.SetParent(_bigBuildingsInactiveParent.transform);
                },
                actionOnGet: obj =>
                {
                    obj.gameObject.SetActive(true);
                    obj.transform.SetParent(_bigBuildingsActiveParent.transform);
                });
            var temp = new PooledBuilding[_environmentData.BuildingInitPoolSize];
            for (int i = 0; i < _environmentData.BuildingInitPoolSize; i++)
                temp[i] = dict[index].Get();
            for (int i = 0; i < _environmentData.BuildingInitPoolSize; i++)
                dict[index].Release(temp[i]);
        }
    }
}
