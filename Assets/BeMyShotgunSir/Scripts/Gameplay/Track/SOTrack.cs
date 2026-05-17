using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    [CreateAssetMenu(fileName = "TrackSO", menuName = "Be My Shotgun, Sir!/Track")]
    public class SOTrack : ScriptableObject
    {
        [field: SerializeField] public string TrackName { get; private set; }
        [field: SerializeField] public int MaxPoolSize { get; private set; }
        [field: SerializeField] public int MaxActiveChunks { get; private set; }
        [field: SerializeField] public RoadChunk[] RoadChunks { get; private set; }
        [field: SerializeField] public RoadChunk[] SpecialRoadChunks { get; private set; }
        [field: SerializeField] public int RoadChunkInitPoolSize { get; private set; }
        [field: SerializeField] public int StraightPercentage { get; private set; }

        [Header("Common Road Chunk Settings")]
        [field: SerializeField] public int MinimumTrackLength { get; private set; }
        [field: SerializeField] public int MaximumTrackLength { get; private set; }

        [Header("Path Road Chunk Settings")]
        [field: SerializeField] public int MinSplitRoadChunkCount { get; private set; }
        [field: SerializeField] public int MaxSplitRoadChunkCount { get; private set; }
    }
}
