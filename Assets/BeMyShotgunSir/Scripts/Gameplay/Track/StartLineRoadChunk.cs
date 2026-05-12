using System.Collections.Generic;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class StartLineRoadChunk : RoadChunk
    {
        public override RoadChunkType Type => RoadChunkType.START_LINE;
        [field: SerializeField] public List<Transform> GridPositions { get; private set; }
    }
}
