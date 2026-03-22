using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public enum RoadChunkPosition { LEFT, MIDDLE, RIGHT }

    public struct GeneratedRoadChunkInfo
    {
        public int index;
        public RoadChunkType type;
        public RoadChunkPosition position;

        public GeneratedRoadChunkInfo(int index, RoadChunkType type, RoadChunkPosition position)
        {
            this.index = index;
            this.type = type;
            this.position = position;
        }
    }

    public class TrackManager : MonoBehaviour
    {
        private enum SpecialRoadChunkIndex { STARTING_CROSSROAD = 0, ENDING_CROSSROAD = 1, START_LINE = 2, STRAIGHT = 3, LEFT_HALF_CIRCLE = 4, RIGHT_HALF_CIRCLE = 5, AUSTIN_SNAKE = 6 }

        [SerializeField] private TrackSeed _trackSeed;
        [SerializeField] private SOTrack _trackData;

        private Queue<GeneratedRoadChunkInfo> _trackBits = new Queue<GeneratedRoadChunkInfo>();
        private Random Rng => _trackSeed.Rng;

        private bool _isGeneratingSplit = false;
        private int _chunksRemainingInCurrentState = 0;

        private int _leftWeight = -1;
        private int _rightWeight = 1;
        private List<int> _possibleTurnWeights = new List<int>() { -2, -1, 0, 1, 2 };
        private int _maxWeight;
        private Dictionary<int, int> _weightToChunkIndexMap = new Dictionary<int, int>();

        private void Start()
        {
            if (_trackSeed == null || _trackData == null)
            {
                Debug.LogError("TrackManager: Missing TrackSeed or SOTrack reference.");
                return;
            }

            // Initial Sequence
            _trackBits.Enqueue(new GeneratedRoadChunkInfo((int)SpecialRoadChunkIndex.START_LINE, RoadChunkType.STRAIGHT, RoadChunkPosition.MIDDLE));
            _trackBits.Enqueue(new GeneratedRoadChunkInfo((int)SpecialRoadChunkIndex.STRAIGHT, RoadChunkType.STRAIGHT, RoadChunkPosition.MIDDLE));

            _isGeneratingSplit = false;
            _chunksRemainingInCurrentState = Rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);

            while (_trackBits.Count < _trackData.QueueBufferSize)
            {
                EnqueueNextSegment();
            }

            _maxWeight = _possibleTurnWeights.Max();
            _weightToChunkIndexMap.Clear();
            for (int i = 0; i < _trackData.RoadChunks.Length; i++)
            {
                int weight = _trackData.RoadChunks[i].TurnWeight;
                if (!_weightToChunkIndexMap.ContainsKey(weight))
                {
                    _weightToChunkIndexMap.Add(weight, i);
                }
            }
        }

        public List<GeneratedRoadChunkInfo> GetGeneratedRoadChunkInfo()
        {
            List<GeneratedRoadChunkInfo> result = new List<GeneratedRoadChunkInfo>();
            if (_trackBits.Count == 0) return result;

            GeneratedRoadChunkInfo nextChunk = _trackBits.Dequeue();
            result.Add(nextChunk);

            // If the popped chunk is part of a split, pop its sibling too
            if (nextChunk.position == RoadChunkPosition.LEFT || nextChunk.position == RoadChunkPosition.RIGHT)
            {
                result.Add(_trackBits.Dequeue());
            }

            EnqueueNextSegment();

            return result;
        }

        public int GetRandomNumberInRange(int min, int max)
        {
            return Rng.Next(min, max);
        }

        private void EnqueueNextSegment()
        {
            if (_chunksRemainingInCurrentState <= 0)
            {
                _isGeneratingSplit = !_isGeneratingSplit;
                if (_isGeneratingSplit) // Common -> Split
                {
                    _trackBits.Enqueue(new GeneratedRoadChunkInfo((int)SpecialRoadChunkIndex.STARTING_CROSSROAD, RoadChunkType.STARTING_CROSSROAD, RoadChunkPosition.MIDDLE));
                    _chunksRemainingInCurrentState = Rng.Next(_trackData.MinSplitRoadChunkCount, _trackData.MaxSplitRoadChunkCount);
                }
                else // Split -> Common
                {
                    _trackBits.Enqueue(new GeneratedRoadChunkInfo((int)SpecialRoadChunkIndex.ENDING_CROSSROAD, RoadChunkType.ENDING_CROSSROAD, RoadChunkPosition.MIDDLE));
                    _chunksRemainingInCurrentState = Rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);
                    _leftWeight = -1;
                    _rightWeight = 1;
                }
                return; //I already added the crossroad for this step
            }

            _chunksRemainingInCurrentState--;
            if (_isGeneratingSplit)
            {
                _trackBits.Enqueue(GenerateSplitChunkInfo(RoadChunkPosition.LEFT));
                _trackBits.Enqueue(GenerateSplitChunkInfo(RoadChunkPosition.RIGHT));
            }
            else
            {
               _trackBits.Enqueue(GenerateCommonChunkInfo());
            }
        }

        private GeneratedRoadChunkInfo GenerateCommonChunkInfo()
        {
            GeneratedRoadChunkInfo result = new GeneratedRoadChunkInfo();
            if (Rng.Next(0, 100) < _trackData.StraightPercentage)
            {
                switch (Rng.Next(0, 4))
                {
                    case 0:
                        result.index = (int)SpecialRoadChunkIndex.STRAIGHT;
                        result.type = RoadChunkType.STRAIGHT;
                        break;
                    case 1:
                        result.index = (int)SpecialRoadChunkIndex.LEFT_HALF_CIRCLE;
                        result.type = RoadChunkType.STRAIGHT;
                        break;
                    case 2:
                        result.index = (int)SpecialRoadChunkIndex.RIGHT_HALF_CIRCLE;
                        result.type = RoadChunkType.STRAIGHT;
                        break;
                    case 3:
                        result.index = (int)SpecialRoadChunkIndex.AUSTIN_SNAKE;
                        result.type = RoadChunkType.STRAIGHT;
                        break;
                }
            }
            else
            {
                result.index = Rng.Next(0, _trackData.RoadChunks.Length);
                result.type = RoadChunkType.TURN;
            }
            result.position = RoadChunkPosition.MIDDLE;
            return result;
        }

        private GeneratedRoadChunkInfo GenerateSplitChunkInfo(RoadChunkPosition position)
        {
            int currentWeight = (position == RoadChunkPosition.LEFT) ? _leftWeight : _rightWeight;
            int targetWeight = (position == RoadChunkPosition.LEFT) ? -1 : 1;
            int maxWeightChangePossible = _chunksRemainingInCurrentState * _maxWeight;

            List<int> possibleWeights = new List<int>();
            foreach (var w in _possibleTurnWeights)
            {
                int projectedWeight = currentWeight + w;
                bool isMergeSafe = (position == RoadChunkPosition.LEFT) ? (projectedWeight <= -1) : (projectedWeight >= 1);
                bool canReachTarget = Mathf.Abs(targetWeight - projectedWeight) <= maxWeightChangePossible;
                if (isMergeSafe && canReachTarget)
                {
                    possibleWeights.Add(w);
                }
            }
            if (possibleWeights.Count == 0)
            {
                Debug.LogError("TrackManager: No possible weights to choose from. This should never happen");
            }

            int selectedWeight = possibleWeights[Rng.Next(0, possibleWeights.Count)];
            if (position == RoadChunkPosition.LEFT)
            {
                _leftWeight += selectedWeight;
            }
            else
            {
                _rightWeight += selectedWeight;
            }

            RoadChunkType chunkType = selectedWeight == 0 ? RoadChunkType.STRAIGHT : RoadChunkType.TURN;
            int chunkIndex = MapWeightToChunkIndex(selectedWeight);
            return new GeneratedRoadChunkInfo(chunkIndex, chunkType, position);
        }

        private int MapWeightToChunkIndex(int weight)
        {
            if (weight == 0)
            {
                return (int)SpecialRoadChunkIndex.STRAIGHT;
            }

            if (_weightToChunkIndexMap.TryGetValue(weight, out int chunkIndex))
            {
                return chunkIndex;
            }

            Debug.LogError($"TrackManager: No prefab found for physical weight {weight}. Defaulting to Straight.");
            return (int)SpecialRoadChunkIndex.STRAIGHT;
        }
    }
}
