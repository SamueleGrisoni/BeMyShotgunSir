using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Be My Shotgun, Sir!/Items")]
    public class SOItem : ScriptableObject
    {
        [Header("Power-Up Spawn Settings")]
        [Tooltip("Chance to spawn a powerUp in a given Area Group.")]
        [field: SerializeField] public int ChanceToSpawnPowerUpPerAreaGroup { get; private set; }

        [Tooltip("Max number of Area Groups that can have PowerUp in a given Road Chunk.")]
        [field: SerializeField] public int MaxAreaGroupsWithPowerUpPerRoadChunk { get; private set; }

        [Header("Obstacle Spawn Settings")]
        [Tooltip("Chance to spawn an obstacle in a given Area Group.")]
        [field: SerializeField] public int ChanceToSpawnObstaclePerAreaGroup { get; private set; }

        [Tooltip("Max number of Area Groups that can have Obstacles in a given Road Chunk.")]
        [field: SerializeField] public int MaxAreaGroupsWithObstaclePerRoadChunk { get; private set; }
    }
}
