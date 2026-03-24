using System;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{

    [Serializable]
    public struct EnvChunkInfo
    {
        [SerializeField] private GameObject _envChunkPrefab;
        [SerializeField] private EnvChunk _envChunkComponent;
        public readonly GameObject EnvChunkPrefab => _envChunkPrefab;
        public readonly EnvChunk EnvChunkComponent => _envChunkComponent;
    }

    [CreateAssetMenu(fileName = "TrackSO", menuName = "Be My Shotgun, Sir!/Track")]
    public class SOTrack : ScriptableObject
    {
        [field: SerializeField] public string TrackName { get; private set; }
        [field: SerializeField] public int QueueBufferSize { get; private set; }
        [field: SerializeField] public int MaxPoolSize { get; private set; }
        [field: SerializeField] public int MaxActiveChunks { get; private set; }
        [field: SerializeField] public RoadChunk[] RoadChunks { get; private set; }
        [field: SerializeField] public RoadChunk[] SpecialRoadChunks { get; private set; }
        [field: SerializeField] public int RoadChunkInitPoolSize { get; private set; }
        [field: SerializeField] public EnvChunkInfo[] EnvChunks { get; private set; }
        [field: SerializeField] public int EnvChunkInitPoolSize { get; private set; }
        [field: SerializeField] public GameObject[] RandomProps { get; private set; }
        [field: SerializeField] public int RandomPropInitPoolSize { get; private set; }
        [field: SerializeField] public int StraightPercentage { get; private set; }

        [Header("Common Road Chunk Settings")]
        [field: SerializeField] public int MinimumTrackLength { get; private set; }
        [field: SerializeField] public int MaximumTrackLength { get; private set; }

        [Header("Path Road Chunk Settings")]
        [field: SerializeField] public int MinSplitRoadChunkCount { get; private set; }
        [field: SerializeField] public int MaxSplitRoadChunkCount { get; private set; }

        private EnvChunkInfo[][] _envChunksBySize;
        public EnvChunkInfo[] GetEnvChunksBySize(EnvChunkSize size)
        {
            if (_envChunksBySize == null) PopulateJaggedArray();
            return _envChunksBySize[(int)size];
        }


        private int[][] _envChunkIndicesAndDimensionBySize;//for each size(index) the number of elements of that size (value)
        public int GetEnvSizePoolStartIndex(EnvChunkSize size)
        {
            if (_envChunkIndicesAndDimensionBySize == null)
                PopulateJaggedArray();

            return _envChunkIndicesAndDimensionBySize[(int)size][1]; // Return the start index of elements for the given size
        }
        public int GetEnvSizePoolEndIndex(EnvChunkSize size)
        {
            if (_envChunkIndicesAndDimensionBySize == null)
                PopulateJaggedArray();

            int[] details = _envChunkIndicesAndDimensionBySize[(int)size];

            if (details[0] == -1) return -1;

            //DANGER not -1 because RNG is exclusive on the upper bound, so we want to return the index of the last element of that size + 1
            return details[1] + details[0];
        }

        private void OrderEnvChunksBySize() => Array.Sort(EnvChunks, (a, b) => a.EnvChunkComponent.Size.CompareTo(b.EnvChunkComponent.Size));

        private void OnEnable() => PopulateJaggedArray();
        private void OnValidate() => PopulateJaggedArray();
        private void PopulateJaggedArray()
        {
            if (EnvChunks == null || EnvChunks.Length == 0) return;

            OrderEnvChunksBySize();

            int sizesCount = (int)EnvChunkSize.Max;
            _envChunksBySize = new EnvChunkInfo[sizesCount][];
            _envChunkIndicesAndDimensionBySize = new int[sizesCount][];

            int currentStartIndex = 0;
            for (int s = 0; s < sizesCount; s++)
            {
                var currentTargetSize = (EnvChunkSize)s;
                int count = 0;

                for (int i = currentStartIndex; i < EnvChunks.Length; i++)
                {
                    if (EnvChunks[i].EnvChunkComponent.Size == currentTargetSize)
                        count++;
                    else
                        break;
                }

                // Se non ci sono chunk di questa taglia, lasciamo _envChunksBySize[s] null
                if (count > 0)
                {
                    _envChunksBySize[s] = new EnvChunkInfo[count];
                    _envChunkIndicesAndDimensionBySize[s] = new int[2] { count, currentStartIndex };

                    for (int j = 0; j < count; j++)
                    {
                        _envChunksBySize[s][j] = EnvChunks[currentStartIndex + j];
                    }
                }
                else
                {
                    // Restituiamo -1 per indicare esplicitamente l'assenza
                    _envChunkIndicesAndDimensionBySize[s] = new int[2] { -1, -1 };
                }

                currentStartIndex += count;
            }
        }
    }
}
