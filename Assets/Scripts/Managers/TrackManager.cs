using System;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoadManager roadManager;
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private Transform car;

    [Header("Config")]
    [SerializeField] private int initialChunks = 5;
    [SerializeField] private float safeZone = 15f;
    [SerializeField] private int seed = 12345;

    private System.Random _rng;

    void Start()
    {
        if (roadManager == null)
        {
            Debug.LogError("Road Generator reference is missing in TrackManager!", this);
        }

        if (buildingManager == null)
        {
            Debug.LogError("Building Manager reference is missing in TrackManager!", this);
        }

        if (car == null)
        {
            Debug.LogError("Car reference is missing in TrackManager!", this);
        }

        _rng = new System.Random(seed);

        for (int i = 0; i < initialChunks; i++)
        {
            SpawnSegment();
        }
    }

    void Update()
    {
        RoadChunk oldest = roadManager.OldestChunk;

        if (roadManager.OldestChunk is null)
        {
            Debug.LogError("[TrackManager] No active road chunks found! Ensure RoadManager is spawning chunks correctly.", this);
            return;
        }

        if (car.position.z > oldest.transform.position.z + oldest.Length + safeZone)
        {
            RecycleSegment(oldest);
            SpawnSegment();
        }
    }

    private void SpawnSegment()
    {
        RoadChunk newChunk = roadManager.SpawnNext();
        buildingManager.SpawnForChunk(newChunk, _rng);
    }

    private void RecycleSegment(RoadChunk chunk)
    {
        buildingManager.RecycleForChunk(chunk);
        roadManager.RecycleOldest();
    }
}
