using UnityEngine;
using System.Collections.Generic;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class EnvironmentSpawner : MonoBehaviour
    {
        private PolygonSpawnArea _currentSpawnArea;

        [Header("Building Prefabs")]
        [SerializeField] private List<Building> _bigBuildingPrefabs;
        [SerializeField] private List<Building> _mediumBuildingPrefabs;
        [SerializeField] private List<Building> _smallBuildingPrefabs;
        [SerializeField] private List<CityProps> _propsPrefabs;
        [SerializeField] private SOEnvironment _environmentData;
        [SerializeField] private TrackSeed _trackSeed;
        private List<Bounds> _spawnPrefabsBounds = new List<Bounds>();
        private Random _rng => _trackSeed.Rng;
        public void PopulateChunk(RoadChunk chunk)
        {
            if (!ValidateInspectorData(chunk))
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
                Debug.LogError("[EnvironmentSpawnerSpanwer] No spawn areas assigned to chunk: " + chunk.name);
                return false;
            }
            return true;
        }

        private void SpawnProps()
        {
            if (_propsPrefabs == null || _propsPrefabs.Count == 0) return;
            //todo use RNG from server to ensure same environment for all players
            int propsToSpawn = _rng.Next(_environmentData.MaxPropsPerSpawnArea / 3, _environmentData.MaxPropsPerSpawnArea + 1);
            for (int i = 0; i < propsToSpawn; i++)
            {
                CityProps randomPrefab = _propsPrefabs[_rng.Next(0, _propsPrefabs.Count - 1)];
                TrySpawnPlaceable(randomPrefab);
            }
            if (_rng.Next(0, 100) < _environmentData.ChanceToSpawnSomethingFunny)
            {
                Debug.Log("Something funny spawned!");
                Instantiate(_propsPrefabs[^1].gameObject,
                    _currentSpawnArea.transform.position + Vector3.up * 50f, default,
                    _currentSpawnArea.transform);
            }
        }

        private void SpawnBuildings()
        {
            SpawnBuildingDimension(_bigBuildingPrefabs, _environmentData.MaxSpawnAttemptsPerBigBuilding);
            SpawnBuildingDimension(_mediumBuildingPrefabs, _environmentData.MaxSpawnAttemptsPerMediumBuilding);
            SpawnBuildingDimension(_smallBuildingPrefabs, _environmentData.MaxSpawnAttemptsPerSmallBuilding);
        }

        private void SpawnBuildingDimension(List<Building> prefabs, int maxFailures)
        {
            if (prefabs == null || prefabs.Count == 0) return;

            int failedAttempts = 0;
            while (failedAttempts < maxFailures)
            {
                //Todo use RNG from server to ensure same environment for all players
                Building randomPrefab = prefabs[_rng.Next(0, prefabs.Count)];

                if (TrySpawnPlaceable(randomPrefab))
                {
                    failedAttempts = 0;
                }
                else
                {
                    failedAttempts++;
                }
            }
        }

        private bool TrySpawnPlaceable(Placeable prefab)
        {
            Bounds areaBounds = _currentSpawnArea.ComputePolygonBounds();
            Bounds prefabBounds = prefab.GetFlatBounds();
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
                        Place(prefab, candidateCenter, lookDir);
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

        private void Place(Placeable prefab, Vector3 worldPos, Vector3 edgeDir)
        {
            var rotation = Quaternion.LookRotation(edgeDir, Vector3.up);
            //Todo spawning should be handle with pooling
            Instantiate(prefab.gameObject, worldPos, rotation, _currentSpawnArea.transform);
        }

        private bool ValidateInspectorData(RoadChunk chunk)
        {
            if (!AreSpawnAreasValid(chunk))
            {
                return false;
            }
            if (_bigBuildingPrefabs == null || _bigBuildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentSpawnerEnvironmentSpanwer] No big building prefabs assigned.");
                return false;
            }
            if (_mediumBuildingPrefabs == null || _mediumBuildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentSpawnerEnvironmentSpanwer] No medium building prefabs assigned.");
                return false;
            }
            if (_smallBuildingPrefabs == null || _smallBuildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentSpawnerEnvironmentSpanwer] No small building prefabs assigned.");
                return false;
            }
            return true;
        }
    }
}
