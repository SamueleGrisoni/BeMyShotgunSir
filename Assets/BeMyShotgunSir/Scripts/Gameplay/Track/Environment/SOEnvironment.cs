using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    [CreateAssetMenu(fileName = "EnvSO", menuName = "Be My Shotgun, Sir!/Environment")]
    public class SOEnvironment : ScriptableObject
    {
        [Header("Layout")]
        [Tooltip("Gap between buildings next to each other (world units).")]
        [field: SerializeField] public float GapX { get; private set; }

        [Tooltip("How far from the road edge to place the row (world units).")]
        [field: SerializeField] public float RoadEdgeOffset { get; private set; }

        [Header("Spawning")]
        [Tooltip("Max number of attempts to find a valid spawn position for a certain building dimension")]
        [field: SerializeField] public int MaxSpawnAttemptsPerBigBuilding { get; private set; }
        [field: SerializeField] public int MaxSpawnAttemptsPerMediumBuilding { get; private set; }
        [field: SerializeField] public int MaxSpawnAttemptsPerSmallBuilding { get; private set; }

        [Tooltip("Max number of props to be spawned in a spawn area")]
        [field: SerializeField] public int MaxPropsPerSpawnArea { get; private set; }
        [field: SerializeField] public int ChanceToSpawnSomethingFunny { get; private set; }
    }
}
