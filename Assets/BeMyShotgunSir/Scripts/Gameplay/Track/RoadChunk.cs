using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using BeMyShotgunSir.Scripts.Gameplay.Track.Items;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public enum RoadChunkType
    {
        STRAIGHT,
        TURN,
        STARTING_CROSSROAD, //fork
        ENDING_CROSSROAD, //junction
        START_LINE,
        FINISH_LINE
    }

    public class RoadChunk : MonoBehaviour
    {
        public virtual RoadChunkType Type { get; set; } = RoadChunkType.TURN;
        public int ChunkNumber { get; set; }
        [SerializeField] private Transform _spawnAnchor;
        public Transform SpawnAnchor => _spawnAnchor != null ? _spawnAnchor : transform;
        [field: SerializeField] public Transform[] NextRoadAnchors { get; private set; }
        [field: SerializeField] public PolygonSpawnArea[] PolygonSpawnArea { get; private set; }

        [field: SerializeField] public int TurnWeight { get; private set; }
        [field: SerializeField] public ItemAreaGroup[] AreaGroup { get; private set; }
    }
}
