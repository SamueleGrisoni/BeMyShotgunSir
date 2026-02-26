using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RoadChunk roadPrefab;
    
    private float _spawnZ = 0f;
    private readonly Queue<RoadChunk> _activeChunks = new Queue<RoadChunk>();
    private readonly Queue<RoadChunk> _pool = new Queue<RoadChunk>();
    
    public RoadChunk OldestChunk => _activeChunks.Count > 0 ? _activeChunks.Peek() : null;
    
    public RoadChunk SpawnNext()
    {
        RoadChunk newChunk;
        
        if (_pool.Count > 0)
        {
            newChunk = _pool.Dequeue();
            newChunk.gameObject.SetActive(true);
        }
        else
        {
            newChunk = Instantiate(roadPrefab);
        }
        
        newChunk.transform.position = Vector3.forward * _spawnZ;
        newChunk.SetPosition(newChunk.transform.position);

        _spawnZ += newChunk.Length;
        _activeChunks.Enqueue(newChunk);

        return newChunk;
    }
    
    public void RecycleOldest()
    {
        if (_activeChunks.Count == 0) return;

        RoadChunk oldChunk = _activeChunks.Dequeue();
        oldChunk.gameObject.SetActive(false);
        _pool.Enqueue(oldChunk);
    }
}
