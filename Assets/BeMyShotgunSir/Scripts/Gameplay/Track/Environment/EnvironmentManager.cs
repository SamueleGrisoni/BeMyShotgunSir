using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        private PolygonSpawnArea _currentSpawnArea;

        [Header("Building Prefabs")]
        [SerializeField] private List<Building> _bigBuildingPrefabs;
        [SerializeField] private List<Building> _mediumBuildingPrefabs;
        [SerializeField] private List<Building> _smallBuildingPrefabs;
        [SerializeField] private SOEnvironment _environmentData;
        private List<Bounds> _spawnBuildingsBounds = new List<Bounds>();

        public void PopulateChunk(RoadChunk chunk)
        {
            if (!AreSpawnAreasValid(chunk))
            {
                return;
            }
            //Todo spawning should be handle with pooling
            if (_bigBuildingPrefabs == null || _bigBuildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentManager] No big building prefabs assigned.");
                return;
            }
            if (_mediumBuildingPrefabs == null || _mediumBuildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentManager] No medium building prefabs assigned.");
                return;
            }
            if (_smallBuildingPrefabs == null || _smallBuildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentManager] No small building prefabs assigned.");
                return;
            }
            foreach (var spawnArea in chunk.PolygonSpawnArea)
            {
                _spawnBuildingsBounds.Clear();
                if (spawnArea is null)
                {
                    continue;
                }
                spawnArea.InvalidateWorldPointsCache();
                _currentSpawnArea = spawnArea;
                SpawnBuildings();
                //SpawnProps();
            }
        }

        public void ClearSpawnedBuildings(RoadChunk chunk)
        {
            if (!AreSpawnAreasValid(chunk))
            {
                return;
            }
            foreach (var area in chunk.PolygonSpawnArea)
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
                Debug.LogError("[EnvironmentManager] No spawn areas assigned to chunk: " + chunk.name);
                return false;
            }
            return true;
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
                Building randomPrefab = prefabs[Random.Range(0, prefabs.Count)];

                if (TrySpawnBuilding(randomPrefab))
                {
                    failedAttempts = 0;
                }
                else
                {
                    failedAttempts++;
                }
            }
        }

        private bool TrySpawnBuilding(Building building)
        {
            Bounds areaBounds = _currentSpawnArea.ComputePolygonBounds();
            Bounds buildingBounds = building.GetFlatBounds();
            float width = buildingBounds.size.x;
            float depth = buildingBounds.size.z;

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
                    Vector3 candidateCenter = new Vector3(cursorX + width * 0.5f, 0f, cursorZ - depth * 0.5f);
                    Bounds candidateBounds = new Bounds(candidateCenter, new Vector3(width, 1f, depth));

                    if (_currentSpawnArea.IsBoundsFullyInsidePolygon(candidateBounds) && !OverlapsExistingBuilding(candidateBounds))
                    {
                        Vector3 lookDir = _currentSpawnArea.GetDirectionToClosestEdge(candidateCenter);
                        PlaceBuilding(building, candidateCenter, lookDir);
                        _spawnBuildingsBounds.Add(candidateBounds);
                        return true;
                    }
                    cursorX += width + _environmentData.GapX;
                }
                cursorZ -= depth + _environmentData.GapX;
            }
            return false;
        }

        private bool OverlapsExistingBuilding(Bounds candidate)
        {
            foreach (var b in _spawnBuildingsBounds)
            {
                Bounds expanded = candidate;
                expanded.Expand(new Vector3(_environmentData.GapX, 0f, _environmentData.GapX));
                if (expanded.Intersects(b))
                    return true;
            }
            return false;
        }

        private void PlaceBuilding(Building building, Vector3 worldPos, Vector3 edgeDir)
        {
            Quaternion rotation = Quaternion.LookRotation(edgeDir, Vector3.up);

            GameObject go = Instantiate(building.gameObject, worldPos, rotation,
                _currentSpawnArea.transform);
            Building instance = go.GetComponent<Building>();

            if (instance == null)
            {
                Debug.LogError($"[EnvironmentManager] Prefab '{building.name}' has no Building component!");
            }
        }
    }
}
