using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [SerializeField] private List<Building> buildingPrefabs;

    private Dictionary<RoadChunk, List<Building>> _chunkToBuildings = new Dictionary<RoadChunk, List<Building>>();
    private Dictionary<int, Queue<Building>> _pool = new Dictionary<int, Queue<Building>>();

    private void Awake()
    {
        for (int i = 0; i < buildingPrefabs.Count; i++)
        {
            buildingPrefabs[i].ID = i;

            if (!_pool.ContainsKey(i))
                _pool.Add(i, new Queue<Building>());
        }
    }

    // Spawns buildings for a given chunk on both sides. Uses the provided RNG for procedural generation.
    public void SpawnForChunk(RoadChunk chunk, System.Random rng)
    {
        List<Building> currentBatch = new List<Building>();

        // Populate Left (true) and Right (false) sides for this specific chunk
        PopulateSide(chunk, true, rng, currentBatch);
        PopulateSide(chunk, false, rng, currentBatch);

        _chunkToBuildings[chunk] = currentBatch;
    }

    private void PopulateSide(RoadChunk chunk, bool isLeft, System.Random rng, List<Building> batch)
    {
        //todo idk why but chuck.transform.position.z is the end of the chunk not the start. @Lore se vuoi darci un occhio, odio già il 3d
        float currentZ = chunk.transform.position.z - chunk.Length;
        float endZ = currentZ + chunk.Length;

        while (currentZ < endZ)
        {
            Building prefab = buildingPrefabs[rng.Next(buildingPrefabs.Count)];
            Building building = GetFromPool(prefab);

            building.transform.rotation = isLeft ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, -90, 0);

            // Check if we fit within the chunk otherwise return to pool and break
            float buildingZSize = building.MeshRenderer.bounds.size.z;
            float zPos = currentZ + (buildingZSize / 2f);
            if (zPos + (buildingZSize / 2f) > endZ)
            {
                building.gameObject.SetActive(false);
                _pool[prefab.ID].Enqueue(building);
                break;
            }

            building.transform.position = new Vector3(ComputeXOffSet(chunk, building, isLeft), 0, zPos);
            batch.Add(building);
            currentZ += buildingZSize;
        }
    }

    private float ComputeXOffSet(RoadChunk chunk, Building building, bool isLeft)
    {
        Bounds bBounds = building.MeshRenderer.bounds;
        Bounds roadBounds = chunk.MeshRenderer.bounds;

        return isLeft
            ? roadBounds.min.x - bBounds.max.x   // Align building right edge to road left edge
            : roadBounds.max.x - bBounds.min.x;  // Align building left edge to road right edge
    }

    private Building GetFromPool(Building prefab)
    {
        if (_pool[prefab.ID].Count > 0)
        {
            Building b = _pool[prefab.ID].Dequeue();
            b.gameObject.SetActive(true);
            b.transform.position = Vector3.zero;
            return b;
        }
        Building newBuilding = Instantiate(prefab);
        newBuilding.ID = prefab.ID;
        newBuilding.transform.position = Vector3.zero;
        return newBuilding;
    }

    public void RecycleForChunk(RoadChunk chunk)
    {
        List<Building> toRecycle = _chunkToBuildings.ContainsKey(chunk) ? _chunkToBuildings[chunk] : null;
        if (toRecycle is null)
        {
            Debug.LogWarning("[BuildingManager] No buildings found for chunk " + chunk.name + "to be recycle", this);
        }
        else
        {
            foreach (var b in toRecycle)
            {
                b.gameObject.SetActive(false);
                _pool[b.ID].Enqueue(b);
            }
            _chunkToBuildings.Remove(chunk);
        }
    }
}
