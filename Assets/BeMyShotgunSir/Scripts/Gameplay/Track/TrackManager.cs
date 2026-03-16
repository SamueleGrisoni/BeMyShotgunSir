using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public enum RoadChunkPosition { LEFT, MIDDLE, RIGHT }
    public enum RoadChunkType {STRAIGHT,TURN,STARTING_CROSSROAD, ENDING_CROSSROAD }

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
        private enum SpecialRoadChunkIndex { STARTING_CROSSROAD = 0, ENDING_CROSSROAD = 1, START_LINE = 2, STRAIGHT = 3 }

        [SerializeField] private TrackSeed _trackSeed;
        [SerializeField] private SOTrack _trackData;
        [SerializeField] private int _targetQueueBuffer = 10; // Adjust how many steps ahead it generates

        private Queue<GeneratedRoadChunkInfo> _trackBits = new Queue<GeneratedRoadChunkInfo>();
        private Random Rng => _trackSeed.Rng;

        // State Machine Variables
        private bool _isGeneratingSplit = false;
        private int _chunksRemainingInCurrentState = 0;

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
            _trackBits.Enqueue(new GeneratedRoadChunkInfo((int)SpecialRoadChunkIndex.STRAIGHT, RoadChunkType.STRAIGHT, RoadChunkPosition.MIDDLE));

            _isGeneratingSplit = false;
            _chunksRemainingInCurrentState = Rng.Next(_trackData.MinimumTrackLength, _trackData.MaximumTrackLength);

            // Fill the initial buffer
            while (_trackBits.Count < _targetQueueBuffer)
            {
                GenerateNextSegment();
            }
        }

        private void GenerateNextSegment()
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
                }
                return; //I already added the crossroad for this step
            }

            _chunksRemainingInCurrentState--;
            if (_isGeneratingSplit)
            {
                GeneratedRoadChunkInfo leftChunkInfo = GetRandomChunkInfo();
                leftChunkInfo.position = RoadChunkPosition.LEFT;
                _trackBits.Enqueue(leftChunkInfo);

                GeneratedRoadChunkInfo rightChunkInfo = GetRandomChunkInfo();
                rightChunkInfo.position = RoadChunkPosition.RIGHT;
                _trackBits.Enqueue(rightChunkInfo);
            }
            else
            {
               GeneratedRoadChunkInfo nextChunkInfo = GetRandomChunkInfo();
               nextChunkInfo.position = RoadChunkPosition.MIDDLE;
               _trackBits.Enqueue(nextChunkInfo);
            }
        }

        private GeneratedRoadChunkInfo GetRandomChunkInfo()
        {
            GeneratedRoadChunkInfo result = new GeneratedRoadChunkInfo();
            if (Rng.Next(0, 100) < _trackData.StraightPercentage)
            {
                result.index = (int)SpecialRoadChunkIndex.STRAIGHT;
                result.type = RoadChunkType.STRAIGHT;
            }
            else
            {
                result.index = Rng.Next(0, _trackData.RoadChunks.Length);
                result.type = RoadChunkType.TURN;
            }
            return result;
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

            GenerateNextSegment();

            return result;
        }
    }
}
