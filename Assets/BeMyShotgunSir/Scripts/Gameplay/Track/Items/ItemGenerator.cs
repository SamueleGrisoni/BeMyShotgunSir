using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public struct GeneratedItemInfo
    {
        public int index;
        public ItemType type;
        public int areaGroupIndex;
        public int spawnPointIndex;

        public GeneratedItemInfo(int index, ItemType type, int areaGroupIndex, int spawnPointIndex = 0)
        {
            this.index = index;
            this.type = type;
            this.areaGroupIndex = areaGroupIndex;
            this.spawnPointIndex = spawnPointIndex;
        }
    }

    public class ItemGenerator : MonoBehaviour
    {
        [SerializeField] private TrackSeed _trackSeed;
        [SerializeField] private SOItem _itemData;

        private bool _isInCommonRoad = true;
        private int _numOfPowerUpSpawnedInRightSplit = 0;
        private int _numOfPowerUpSpawnedInLeftSplit = 0;

        private Random Rng => _trackSeed.Rng;

        private void Start()
        {
            if (_trackSeed == null || _itemData == null)
            {
                Debug.LogError("TrackManager: Missing TrackSeed or SOItem reference.");
            }
        }

        public List<GeneratedItemInfo> GenerateItemsForRoadChunk(RoadChunkPosition roadChunkPosition)
        {
            if (roadChunkPosition == RoadChunkPosition.MIDDLE)
            {
                if (!_isInCommonRoad)
                {
                    //Debug.Log("Entering COMMON ROAD");
                    _isInCommonRoad = true;
                    _numOfPowerUpSpawnedInLeftSplit = 0;
                    _numOfPowerUpSpawnedInRightSplit = 0;
                }
                return GenerateObstacle();
            }
            if (_isInCommonRoad)
            {
                //Debug.Log("Entering SPLIT ROAD");
                _isInCommonRoad = false;
            }
            return GeneratePowerUp(roadChunkPosition);
        }

        private List<GeneratedItemInfo> GenerateObstacle()
        {
            List<GeneratedItemInfo> generatedObstacles = new List<GeneratedItemInfo>();
            int numOfAreaGroupWithObstacle = 0;

            for (int areaGroupIndex = 0; areaGroupIndex < _itemData.NumberOfAreaGroupPerChunk; areaGroupIndex++, numOfAreaGroupWithObstacle++)
            {
                if (Rng.NextDouble() < _itemData.ChanceToSpawnObstaclePerAreaGroup && numOfAreaGroupWithObstacle < _itemData.MaxAreaGroupsWithObstaclePerRoadChunk)
                {
                    if(Rng.NextDouble() < _itemData.ChanceToSpawnTwoObstaclesInAreaGroup)
                    {
                        generatedObstacles.Add(new GeneratedItemInfo(Rng.Next(0, _itemData.ObstacleItems.Length), ItemType.OBSTACLE, areaGroupIndex, 0));
                        generatedObstacles.Add(new GeneratedItemInfo(Rng.Next(0, _itemData.ObstacleItems.Length), ItemType.OBSTACLE, areaGroupIndex, _itemData.NumberOfItemSpawnAreaPerAreaGroup-1));
                    }
                    else
                    {
                        generatedObstacles.Add(new GeneratedItemInfo(Rng.Next(0, _itemData.ObstacleItems.Length), ItemType.OBSTACLE, areaGroupIndex, Rng.Next(0, _itemData.NumberOfItemSpawnAreaPerAreaGroup)));
                    }
                }
            }

            return generatedObstacles;
        }

        private List<GeneratedItemInfo> GeneratePowerUp(RoadChunkPosition position)
        {
            List<GeneratedItemInfo> generatedPowerUps = new List<GeneratedItemInfo>();
            for (int areaGroupIndex = 0; areaGroupIndex < _itemData.NumberOfAreaGroupPerChunk; areaGroupIndex++)
            {
                if (Rng.NextDouble() < _itemData.ChanceToSpawnPowerUpPerAreaGroup)
                {
                    if (position == RoadChunkPosition.LEFT && _numOfPowerUpSpawnedInLeftSplit < _itemData.MaxNumberOfPowerUpPerSplit)
                    {
                        generatedPowerUps.Add(new GeneratedItemInfo(Rng.Next(0, _itemData.PowerUpItems.Length), ItemType.POWER_UP, areaGroupIndex));
                        _numOfPowerUpSpawnedInLeftSplit++;
                    }
                    else if (position == RoadChunkPosition.RIGHT && _numOfPowerUpSpawnedInRightSplit < _itemData.MaxNumberOfPowerUpPerSplit)
                    {
                        generatedPowerUps.Add(new GeneratedItemInfo(Rng.Next(0, _itemData.PowerUpItems.Length), ItemType.POWER_UP, areaGroupIndex));
                        _numOfPowerUpSpawnedInRightSplit++;
                    }
                }
            }
            return generatedPowerUps;
        }
    }
}
