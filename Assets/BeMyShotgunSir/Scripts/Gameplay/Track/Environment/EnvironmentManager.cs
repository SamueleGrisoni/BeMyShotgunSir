using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<PolygonSpawnArea> _spawnAreas;
        private PolygonSpawnArea _currentSpawnArea;

        [Header("Building Prefabs")]
        [SerializeField] private List<Building> _buildingPrefabs = new List<Building>();

        [Header("Layout")]
        [Tooltip("Gap between buildings next to each other (world units).")]
        [SerializeField] private float _gapX = 0.5f;

        [Tooltip("How far from the road edge to place the row (world units).")]
        [SerializeField] private float _roadEdgeOffset = 0.2f;

        private List<Bounds> _spawnBuildingsBounds = new List<Bounds>();


        [ContextMenu("Fill Area")]
        public void FillArea()
        {
            if (_spawnAreas == null || _spawnAreas.Count == 0)
            {
                Debug.LogWarning("[EnvironmentManager] No spawn areas assigned.");
                return;
            }
            if (_buildingPrefabs == null || _buildingPrefabs.Count == 0)
            {
                Debug.LogWarning("[EnvironmentManager] No building prefabs assigned.");
                return;
            }

            _spawnBuildingsBounds.Clear();

            foreach (var area in _spawnAreas)
            {
                if (area == null) continue;
                _currentSpawnArea = area;
                SpawnBuildings();
            }
            _currentSpawnArea = null;
        }

        private void SpawnBuildings()
        {
            Bounds areaBounds = _currentSpawnArea.ComputePolygonBounds();

            float startX = areaBounds.min.x;
            float endX = areaBounds.max.x;
            float startZ = areaBounds.min.z;
            float endZ = areaBounds.max.z;

            float cursorZ = endZ;

            while (cursorZ > startZ)
            {
                float rowDepth = 0f;
                float cursorX = startX;

                while (cursorX < endX)
                {
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

        /*private void TrySpawnBuildingAlongEdge(Vector3 worldStart, Vector3 worldEnd)
        {
            Vector3 edgeDir    = (worldEnd - worldStart).normalized;
            Vector3 edgeNormal = new Vector3(-edgeDir.z, 0f, edgeDir.x);
            float   edgeLength = Vector3.Distance(worldStart, worldEnd);

            var candidates = _buildingPrefabs
                .Where(b => b.GetFlatBounds().size.x <= edgeLength)
                .ToList();

            if (candidates.Count == 0) return;

            float cursor = 0f;
            while (cursor < edgeLength)
            {
                Building prefab   = candidates[Random.Range(0, candidates.Count)];
                float    width    = prefab.GetFlatBounds().size.x;
                float    depth    = prefab.GetFlatBounds().size.z;

                if (cursor + width > edgeLength) break;

                float   t        = (cursor + width * 0.5f) / edgeLength;
                Vector3 spawnPos = Vector3.Lerp(worldStart, worldEnd, t)
                    + edgeNormal * (_roadEdgeOffset + depth * 0.5f);

                // Simple AABB in world space (approximate — good enough for a corridor)
                Bounds worldBounds = new Bounds(spawnPos,
                    new Vector3(width, 1f, depth));

                if (!OverlapsExistingBuilding(worldBounds))
                {
                    PlaceBuilding(prefab, spawnPos, edgeDir);
                    _spawnBuildingsBounds.Add(worldBounds);
                }

                cursor += width + _gapX;
            }
        }*/

    }
}
