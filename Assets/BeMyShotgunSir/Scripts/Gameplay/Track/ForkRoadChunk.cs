using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class ForkRoadChunk : RoadChunk
    {
        public override RoadChunkType Type => RoadChunkType.STARTING_CROSSROAD;
        [field: SerializeField] public GameObject LeftBarrierObject { get; private set; }
        [field: SerializeField] public GameObject RightBarrierObject { get; private set; }
    }
}
