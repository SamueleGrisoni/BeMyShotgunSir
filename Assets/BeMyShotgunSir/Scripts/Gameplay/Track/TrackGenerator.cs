using System;
using System.Collections.Generic;
using System.Linq;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using BeMyShotgunSir.Scripts.Utils;
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
    #endregion

    public class TrackGenerator : MonoBehaviour
    {
        private enum SpecialRoadChunkIndex { STARTING_CROSSROAD = 0, ENDING_CROSSROAD = 1, START_LINE = 2, STRAIGHT = 3, LEFT_HALF_CIRCLE = 4, RIGHT_HALF_CIRCLE = 5, AUSTIN_SNAKE = 6 }

        [SerializeField] private SOTrack _trackData;
        [SerializeField] private ItemGenerator _itemGenerator;

        private Queue<GeneratedRoadChunkInfoWithItems> _trackBits = new Queue<GeneratedRoadChunkInfoWithItems>();
        private Random _rng;

        private int _chunksRemainingInCurrentState = 0;

        private int _leftWeight = -1;
        private int _rightWeight = 1;
        private List<int> _possibleTurnWeights = new List<int>() { -4, -3, -2, -1, 0, 1, 2, 3, 4 };
        private int _maxWeight;
        private Dictionary<int, int> _weightToChunkIndexMap = new Dictionary<int, int>();

        public static event Action<List<GeneratedRoadChunkInfoWithItems>> OnSplitGenerated;

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
                Log.ELazy(() => "TrackManager: Missing SOTrack reference.", this);
                return;
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

            GenerateInitialSequence();
        }

        public List<GeneratedRoadChunkInfoWithItems> PeekStartFinishLine()
        {
            if (_trackBits.Count == 0)
            {
                Log.ELazy(() => "PeekStartFinishLine called but trackBits is empty!", this);
                throw new EarlyExitException();
            }
            if (_trackBits.Peek().roadChunkInfo.index != (int)SpecialRoadChunkIndex.START_LINE)
            {
                Log.ELazy(() => "PeekStartFinishLine called but the first chunk is not the start line!", this);
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

            GenerateNextSequence(nextChunk.roadChunkInfo);

            return result;
        }

        private void GenerateInitialSequence()
        {
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
            _chunksRemainingInCurrentState = _rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);
            EnqueueCommonSegment();
            _chunksRemainingInCurrentState = _rng.Next(_trackData.MinSplitRoadChunkCount, _trackData.MaxSplitRoadChunkCount);
            EnqueueSplitSegment();
        }

        private void GenerateFinalSequence()
        {
            _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(
                new GeneratedRoadChunkInfo(
                    (int)SpecialRoadChunkIndex.STRAIGHT,
                    RoadChunkType.STRAIGHT,
                    RoadChunkPosition.MIDDLE),
                new List<GeneratedItemInfo>()));
            _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(
                new GeneratedRoadChunkInfo(
                    (int)SpecialRoadChunkIndex.START_LINE, //todo this should be a new prefab with a finish line trigger or something
                    RoadChunkType.STRAIGHT,
                    RoadChunkPosition.MIDDLE),
                new List<GeneratedItemInfo>()));
        }

        private void GenerateNextSequence(GeneratedRoadChunkInfo lastDequeuedChunk)
        {
            if (lastDequeuedChunk.type == RoadChunkType.ENDING_CROSSROAD)
            {
                _chunksRemainingInCurrentState = _rng.Next(_trackData.MinSplitRoadChunkCount, _trackData.MaxSplitRoadChunkCount);
                EnqueueSplitSegment();
            }
            else if (lastDequeuedChunk.type == RoadChunkType.STARTING_CROSSROAD)
            {
                _chunksRemainingInCurrentState = _rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);
                EnqueueCommonSegment();
            }
        }

        private void EnqueueCommonSegment()
        {
            while (_chunksRemainingInCurrentState > 0)
            {
                _trackBits.Enqueue(new GeneratedRoadChunkInfoWithItems(GenerateCommonChunkInfo(), _itemGenerator.GenerateItemsForRoadChunk(RoadChunkPosition.MIDDLE)));
                _chunksRemainingInCurrentState--;
            }
        }

        private void EnqueueSplitSegment()
        {
            var splitSegmentChunks = new Queue<GeneratedRoadChunkInfoWithItems>();
            splitSegmentChunks.Enqueue(new GeneratedRoadChunkInfoWithItems(
                new GeneratedRoadChunkInfo(
                    (int)SpecialRoadChunkIndex.STARTING_CROSSROAD,
                    RoadChunkType.STARTING_CROSSROAD,
                    RoadChunkPosition.MIDDLE),
                new List<GeneratedItemInfo>()));


            while (_chunksRemainingInCurrentState > 0)
            {
                _chunksRemainingInCurrentState--;
                GeneratedRoadChunkInfo leftChunkInfo = GenerateSplitChunkInfo(RoadChunkPosition.LEFT);
                List<GeneratedItemInfo> powerUpsForLeftSplit = _itemGenerator.GenerateItemsForRoadChunk(RoadChunkPosition.LEFT);
                splitSegmentChunks.Enqueue(new GeneratedRoadChunkInfoWithItems(leftChunkInfo, powerUpsForLeftSplit));

                GeneratedRoadChunkInfo rightChunkInfo = GenerateSplitChunkInfo(RoadChunkPosition.RIGHT);
                List<GeneratedItemInfo> powerUpsForRightSplit = _itemGenerator.GenerateItemsForRoadChunk(RoadChunkPosition.RIGHT);
                splitSegmentChunks.Enqueue(new GeneratedRoadChunkInfoWithItems(rightChunkInfo, powerUpsForRightSplit));
            }
            _leftWeight = -1;
            _rightWeight = 1;

            splitSegmentChunks.Enqueue(new GeneratedRoadChunkInfoWithItems(
                new GeneratedRoadChunkInfo(
                    (int)SpecialRoadChunkIndex.ENDING_CROSSROAD,
                    RoadChunkType.ENDING_CROSSROAD,
                    RoadChunkPosition.MIDDLE),
                    new List<GeneratedItemInfo>()));
            OnSplitGenerated?.Invoke(splitSegmentChunks.ToList());
            foreach (GeneratedRoadChunkInfoWithItems splitSegmentChunk in splitSegmentChunks)
                _trackBits.Enqueue(splitSegmentChunk);
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
                Log.ELazy(() => "TrackManager: No possible weights to choose from. This should never happen", this);
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

            Log.ELazy(() => $"TrackManager: No prefab found for physical weight {weight}. Defaulting to Straight.", this);
            return (int)SpecialRoadChunkIndex.STRAIGHT;
        }
    }
}
