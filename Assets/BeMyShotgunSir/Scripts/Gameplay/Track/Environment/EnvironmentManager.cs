using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        private PolygonSpawnArea _currentSpawnArea;

        [Header("Building Prefabs")]
        [SerializeField] private List<Building> _buildingPrefabs = new List<Building>();

        [Header("Layout")]
        [Tooltip("Gap between buildings next to each other (world units).")]
        [SerializeField] private float _gapX = 0.5f;

        [Tooltip("How far from the road edge to place the row (world units).")]
        [SerializeField] private float _roadEdgeOffset = 0.2f;

        private List<Bounds> _spawnBuildingsBounds = new List<Bounds>();

        public void PopulateChunk(RoadChunk chunk)
        {
            if (!AreSpawnAreasValid(chunk))
            {
                return;
            }
            //Todo spawning should be handle with pooling
            if (_buildingPrefabs == null || _buildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentManager] No building prefabs assigned.");
                return;
            }
            _spawnBuildingsBounds.Clear();
            foreach (var spawnArea in chunk.PolygonSpawnArea)
            {
                if (spawnArea is null)
                {
                    continue;
                }
                spawnArea.InvalidateWorldPointsCache();
                _currentSpawnArea = spawnArea;
                SpawnBuildings();
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
            _spawnBuildingsBounds.Clear();
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
            Bounds areaBounds = _currentSpawnArea.ComputePolygonBounds();

            float startX = areaBounds.min.x;
            float endX = areaBounds.max.x;
            float startZ = areaBounds.min.z;
            float endZ = areaBounds.max.z;

            float cursorZ = endZ - _roadEdgeOffset;
            while (cursorZ > startZ)
            {
                float rowDepth = 0f;
                float cursorX = startX + _roadEdgeOffset;
                while (cursorX < endX)
                {
                    //todo refactor logic to use game RNG
                    Building buildingPrefab = _buildingPrefabs[Random.Range(0, _buildingPrefabs.Count)];
                    Bounds buildingBounds = buildingPrefab.GetFlatBounds();
                    float width = buildingBounds.size.x;
                    float depth = buildingBounds.size.z;

                    Vector3 candidateCenter = new Vector3(cursorX + width * 0.5f, 0f, cursorZ - depth * 0.5f);
                    Bounds candidateBounds = new Bounds(candidateCenter, new Vector3(width, 1f, depth));

                    if (_currentSpawnArea.IsBoundsFullyInsidePolygon(candidateBounds) && !OverlapsExistingBuilding(candidateBounds))
                    {
                        PlaceBuilding(buildingPrefab, candidateCenter, Vector3.forward);
                        _spawnBuildingsBounds.Add(candidateBounds);

                        if (depth > rowDepth) rowDepth = depth;
                    }
                    cursorX += width + _gapX;
                }

                float advance = rowDepth > 0f ? rowDepth + _gapX : _gapX;
                Debug.Log("Advance cursorZ by: " + advance);
                cursorZ -= advance;
            }
        }

        private bool OverlapsExistingBuilding(Bounds candidate)
        {
            foreach (var b in _spawnBuildingsBounds)
            {
                Bounds expanded = candidate;
                expanded.Expand(new Vector3(_gapX, 0f, _gapX));
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
