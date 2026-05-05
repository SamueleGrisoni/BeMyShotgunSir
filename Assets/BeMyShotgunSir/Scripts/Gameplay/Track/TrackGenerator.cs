using System;
using System.Collections.Generic;
using System.Linq;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public enum RoadChunkPosition { LEFT, MIDDLE, RIGHT }

    #region TrackGenerator Structs
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

    public struct GeneratedRoadChunkInfoWithItems
    {
        public GeneratedRoadChunkInfo roadChunkInfo;
        public List<GeneratedItemInfo> itemsToSpawn;
        public GeneratedRoadChunkInfoWithItems(GeneratedRoadChunkInfo roadChunkInfo, List<GeneratedItemInfo> itemsToSpawn)
        {
            this.roadChunkInfo = roadChunkInfo;
            this.itemsToSpawn = itemsToSpawn;
        }
    }

    public struct SplitSegmentInfo
    {
        public int splitLength; // in number of chunks, not including the starting and ending crossroad
    }
    #endregion

    public class TrackGenerator : MonoBehaviour
    {
        private enum SpecialRoadChunkIndex { STARTING_CROSSROAD = 0, ENDING_CROSSROAD = 1, START_LINE = 2, STRAIGHT = 3, LEFT_HALF_CIRCLE = 4, RIGHT_HALF_CIRCLE = 5, AUSTIN_SNAKE = 6 }

        [SerializeField] private SOTrack _trackData;
        [SerializeField] private ItemGenerator _itemGenerator;

        private Queue<GeneratedRoadChunkInfoWithItems> _trackBits = new Queue<GeneratedRoadChunkInfoWithItems>();
        private Random _rng;

        private bool _isGeneratingSplit = false;
        private int _chunksRemainingInCurrentState = 0;

        private int _leftWeight = -1;
        private int _rightWeight = 1;
        private List<int> _possibleTurnWeights = new List<int>() { -4, -3, -2, -1, 0, 1, 2, 3, 4 };
        private int _maxWeight;
        private Dictionary<int, int> _weightToChunkIndexMap = new Dictionary<int, int>();

        public static event Action<SplitSegmentInfo> OnSplitGenerated;

        public void Init(int seed)
        {
            _rng = new Random(seed);
            _itemGenerator.Init(seed);
            Initialize();
        }
        private void Initialize()
        {
            if (_trackData == null)
            {
                Debug.LogError("TrackManager: Missing SOTrack reference.");
                return;
            }

            // Initial Sequence
            _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(
                new GeneratedRoadChunkInfo(
                    (int)SpecialRoadChunkIndex.START_LINE,
                    RoadChunkType.STRAIGHT,
                    RoadChunkPosition.MIDDLE),
                new List<GeneratedItemInfo>()));
            _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(
                new GeneratedRoadChunkInfo(
                    (int)SpecialRoadChunkIndex.STRAIGHT,
                    RoadChunkType.STRAIGHT,
                    RoadChunkPosition.MIDDLE),
                new List<GeneratedItemInfo>()));

            _isGeneratingSplit = false;
            _chunksRemainingInCurrentState = _rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);

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
                    //Debug.Log("Added " + _trackData.RoadChunks[i].name + "with weight " + _trackData.RoadChunks[i].TurnWeight + " at index " + i);
                    _weightToChunkIndexMap.Add(weight, i);
                }
            }
        }

        public List<GeneratedRoadChunkInfoWithItems> PeekStartFinishLine()
        {
            if (_trackBits.Count == 0)
            {
                Debug.LogError("[TrackManager Server] PeekStartFinishLine called but trackBits is empty!");
                throw new EarlyExitException();
            }
            if (_trackBits.Peek().roadChunkInfo.index != (int)SpecialRoadChunkIndex.START_LINE)
            {
                Debug.LogError("[TrackManager Server] PeekStartFinishLine called but the first chunk is not the start line!");
                throw new EarlyExitException();
            }
            return new List<GeneratedRoadChunkInfoWithItems>() { _trackBits.Peek() };
        }

        public List<GeneratedRoadChunkInfoWithItems> GetGeneratedRoadChunkInfoWithItems()
        {
            var result = new List<GeneratedRoadChunkInfoWithItems>();
            if (_trackBits.Count == 0) return result;

            GeneratedRoadChunkInfoWithItems nextChunk = _trackBits.Dequeue();
            result.Add(nextChunk);

            // If the popped chunk is part of a split, pop its sibling too
            if (nextChunk.roadChunkInfo.position == RoadChunkPosition.LEFT || nextChunk.roadChunkInfo.position == RoadChunkPosition.RIGHT)
            {
                result.Add(_trackBits.Dequeue());
            }

            EnqueueNextSegment();

            return result;
        }

        private void EnqueueNextSegment()
        {
            if (_chunksRemainingInCurrentState <= 0)
            {
                _isGeneratingSplit = !_isGeneratingSplit;
                OnSplitGenerated?.Invoke(new SplitSegmentInfo() { splitLength = _chunksRemainingInCurrentState });
                if (_isGeneratingSplit) // Common -> Split
                {
                    _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(
                            new GeneratedRoadChunkInfo(
                                (int)SpecialRoadChunkIndex.STARTING_CROSSROAD,
                                RoadChunkType.STARTING_CROSSROAD,
                                RoadChunkPosition.MIDDLE),
                            new List<GeneratedItemInfo>()));
                    _chunksRemainingInCurrentState = _rng.Next(_trackData.MinSplitRoadChunkCount, _trackData.MaxSplitRoadChunkCount);
                }
                else // Split -> Common
                {
                    _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(
                        new GeneratedRoadChunkInfo(
                                (int)SpecialRoadChunkIndex.ENDING_CROSSROAD,
                                RoadChunkType.ENDING_CROSSROAD,
                                RoadChunkPosition.MIDDLE),
                        new List<GeneratedItemInfo>()));
                    _chunksRemainingInCurrentState = _rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);
                    _leftWeight = -1;
                    _rightWeight = 1;
                }
                return; //I already added the crossroad for this step
            }

            _chunksRemainingInCurrentState--;
            if (_isGeneratingSplit)
            {
                GeneratedRoadChunkInfo leftChunkInfo = GenerateSplitChunkInfo(RoadChunkPosition.LEFT);
                List<GeneratedItemInfo> powerUpsForLeftSplit = _itemGenerator.GenerateItemsForRoadChunk(RoadChunkPosition.LEFT);
                _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(leftChunkInfo, powerUpsForLeftSplit));

                GeneratedRoadChunkInfo rightChunkInfo = GenerateSplitChunkInfo(RoadChunkPosition.RIGHT);
                List<GeneratedItemInfo> powerUpsForRightSplit = _itemGenerator.GenerateItemsForRoadChunk(RoadChunkPosition.RIGHT);
                _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(rightChunkInfo, powerUpsForRightSplit));
            }
            else
            {
                _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(GenerateCommonChunkInfo(), _itemGenerator.GenerateItemsForRoadChunk(RoadChunkPosition.MIDDLE)));
            }
        }

        private GeneratedRoadChunkInfo GenerateCommonChunkInfo()
        {
            var result = new GeneratedRoadChunkInfo();
            if (_rng.Next(0, 100) < _trackData.StraightPercentage)
            {
                switch (_rng.Next(0, 4))
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
                    default:
                        break;
                }
            }
            else
            {
                result.index = _rng.Next(0, _trackData.RoadChunks.Length);
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

            var possibleWeights = new List<int>();
            foreach (int w in _possibleTurnWeights)
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

            int selectedWeight = possibleWeights[_rng.Next(0, possibleWeights.Count)];
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
