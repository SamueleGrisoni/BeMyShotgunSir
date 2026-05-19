using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    [CreateAssetMenu(fileName = "ItemSO", menuName = "Be My Shotgun, Sir!/Items")]
    public class SOItem : ScriptableObject
    {
        [field: SerializeField] public int NumberOfAreaGroupPerChunk { get; private set; }
        [field: SerializeField] public int NumberOfItemSpawnAreaPerAreaGroup { get; private set; }

        [Header("Power-Up Spawn Settings")]
        [Tooltip("Chance to spawn a powerUp in a given Area Group. The same powerUp will be spawned in all ItemArea of the same Area Group")]
        [field: SerializeField] public float ChanceToSpawnPowerUpPerAreaGroup { get; private set; }

        [Tooltip("Max number of powerUps that can be spawned in a given Split")]
        [field: SerializeField] public int MaxNumberOfPowerUpPerSplit { get; private set; }

        [Header("Obstacle Spawn Settings")]
        [Tooltip("Chance to spawn an obstacle in a given Area Group. Only 1 obstacle can be spawned per Area Group.")]
        [field: SerializeField] public float ChanceToSpawnObstaclePerAreaGroup { get; private set; }

        [Tooltip("Max number of Obstacle that can be spawn in a given Road Chunk.")]
        [field: SerializeField] public int MaxAreaGroupsWithObstaclePerRoadChunk { get; private set; }

        [Tooltip("Percentage of spawning 2 obstacles instead of 1 when an obstacle is spawned in an Area Group.")]
        [field: SerializeField] public float ChanceToSpawnTwoObstaclesInAreaGroup { get; private set; }

        [Header("Item Prefabs")]
        [field: SerializeField] public ObstacleSpawner[] ObstacleItems { get; private set; }

        [Tooltip("Single GenericPowerUp prefab — the networked shell. Must be registered with FishNet's NetworkObject list.")]
        [field: SerializeField] public GenericPowerUp GenericPowerUpPrefab { get; private set; }
        [field: SerializeField] public PowerUp[] PowerUpType { get; private set; }
    }
}
