using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class StartFinishLineRoadChunk : RoadChunk
    {
        [field: SerializeField] public Transform[] GridPositions { get; private set; }
    }
}
