using UnityEngine;
using System.Collections.Generic;
using Random = System.Random;
using BeMyShotgunSir.Scripts.Utils;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class EnvironmentSpawner : MonoBehaviour
    {
        private bool _log = true;
        private PolygonSpawnArea _currentSpawnArea;
        [SerializeField] private SOEnvironment _environmentData;
        [SerializeField] private EnvironmentPooler _environmentPooler;
        private List<Bounds> _spawnPrefabsBounds = new List<Bounds>();
        private Random _rng;

        public void Init(int seed)
        {
            _rng = new Random(seed);
            if (!_environmentData)
            {
                Log.WLazy(() => "[EnvironmentSpawner] No environment data assigned.", this);
                return;
            }
            if(!_environmentPooler)
            {
                Log.WLazy(() => "[EnvironmentSpawner] No environment pooler assigned.", this);
                return;
            }
            _environmentPooler.SetEnvironmentData(_environmentData);
        }


        public void PopulateChunk(RoadChunk chunk)
        {
            if (!AreSpawnAreasValid(chunk))
            {
                return;
            }
            foreach (PolygonSpawnArea spawnArea in chunk.PolygonSpawnArea)
            {
                _spawnPrefabsBounds.Clear();
                if (spawnArea is null)
                {
                    continue;
                }
                spawnArea.InvalidateWorldPointsCache();
                _currentSpawnArea = spawnArea;
                SpawnBuildings();
                SpawnProps();
            }
        }

        public void ClearSpawnedProps(RoadChunk chunk)
        {
            if (!AreSpawnAreasValid(chunk))
            {
                return;
            }
            foreach (PolygonSpawnArea area in chunk.PolygonSpawnArea)
            {
                if (area is null) continue;
                foreach (Transform child in area.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private bool AreSpawnAreasValid(RoadChunk chunk)
        {
            if (chunk.PolygonSpawnArea == null || chunk.PolygonSpawnArea.Length == 0)
            {
                Log.WLazy(() => "[EnvironmentSpawnerSpanwer] No spawn areas assigned to chunk: " + chunk.name, this);
                return false;
            }
            return true;
        }

        private void SpawnProps()
        {
            int propsToSpawn = _rng.Next(_environmentData.MaxPropsPerSpawnArea / 3, _environmentData.MaxPropsPerSpawnArea + 1);
            for (int i = 0; i < propsToSpawn; i++)
            {
                PooledCityProps pooledProps = _environmentPooler
                    .GetPropsPrefab(_rng.Next(0, _environmentData.CityProps.Length - 1));

                if (!TrySpawnPlaceable(pooledProps.Component))
                    pooledProps.ReturnToPool();
            }
            //todo fix easter egg
            /*if (_rng.Next(0, 100) < _environmentData.ChanceToSpawnSomethingFunny)
            {
                Log.DLazy(() => "Something funny spawned!", this, _log);
                Instantiate(_propsPrefabs[^1].gameObject,
                    _currentSpawnArea.transform.position + Vector3.up * 50f, default,
                    _currentSpawnArea.transform);
            }*/
        }

        private void SpawnBuildings()
        {
            SpawnBuildingDimension(
                _environmentData.BigBuildings,
                _environmentData.MaxSpawnAttemptsPerBigBuilding,
                i => _environmentPooler.GetBigBuildingPooled(i));

            SpawnBuildingDimension(
                _environmentData.MediumBuildings,
                _environmentData.MaxSpawnAttemptsPerMediumBuilding,
                i => _environmentPooler.GetMediumBuildingPooled(i));

            SpawnBuildingDimension(
                _environmentData.SmallBuildings,
                _environmentData.MaxSpawnAttemptsPerSmallBuilding,
                i => _environmentPooler.GetSmallBuildingPooled(i));
        }

        private void SpawnBuildingDimension(Building[] prefabs, int maxFailures, System.Func<int, PooledBuilding> getFromPool)
        {
            if (prefabs == null || prefabs.Length == 0) return;

            int failedAttempts = 0;
            while (failedAttempts < maxFailures)
            {
                int prefabIndex = _rng.Next(0, prefabs.Length);
                PooledBuilding pooledBuilding = getFromPool(prefabIndex);

                if (TrySpawnPlaceable(pooledBuilding.Component))
                {
                    failedAttempts = 0;
                }
                else
                {
                    pooledBuilding.ReturnToPool();
                    failedAttempts++;
                }
            }
        }

        private bool TrySpawnPlaceable(Placeable instance)
        {
            Bounds areaBounds = _currentSpawnArea.ComputePolygonBounds();
            Bounds prefabBounds = instance.GetFlatBounds();
            float width = prefabBounds.size.x;
            float depth = prefabBounds.size.z;

            float startX = areaBounds.min.x;
            float endX = areaBounds.max.x;
            float startZ = areaBounds.min.z;
            float endZ = areaBounds.max.z;

            float cursorZ = endZ - _environmentData.RoadEdgeOffset;

            while (cursorZ > startZ)
            {
                float cursorX = startX + _environmentData.RoadEdgeOffset;
                while (cursorX < endX)
                {
                    var candidateCenter = new Vector3(cursorX + width * 0.5f, 0f, cursorZ - depth * 0.5f);
                    var candidateBounds = new Bounds(candidateCenter, new Vector3(width, 1f, depth));

                    if (_currentSpawnArea.IsBoundsFullyInsidePolygon(candidateBounds) && !OverlapsExistingPrefabs(candidateBounds))
                    {
                        Vector3 lookDir = _currentSpawnArea.GetDirectionToClosestEdge(candidateCenter);
                        Place(instance, candidateCenter, lookDir);
                        _spawnPrefabsBounds.Add(candidateBounds);
                        return true;
                    }
                    cursorX += width + _environmentData.GapX;
                }
                cursorZ -= depth + _environmentData.GapX;
            }
            return false;
        }

        private bool OverlapsExistingPrefabs(Bounds candidate)
        {
            foreach (Bounds b in _spawnPrefabsBounds)
            {
                Bounds expanded = candidate;
                expanded.Expand(new Vector3(_environmentData.GapX, 0f, _environmentData.GapX));
                if (expanded.Intersects(b))
                    return true;
            }
            return false;
        }

        private void Place(Placeable instance, Vector3 worldPos, Vector3 edgeDir)
        {
            instance.transform.SetPositionAndRotation(worldPos, Quaternion.LookRotation(edgeDir, Vector3.up));
            instance.transform.SetParent(_currentSpawnArea.transform, true);
            instance.gameObject.SetActive(true);
        }
    }
}
