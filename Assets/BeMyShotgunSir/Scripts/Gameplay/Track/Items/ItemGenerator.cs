using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public enum ItemType { POWER_UP, OBSTACLE }
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
        [SerializeField] private SOItem _itemData;

        private bool _isInCommonRoad = true;
        private int _numOfPowerUpSpawnedInRightSplit = 0;
        private int _numOfPowerUpSpawnedInLeftSplit = 0;

        private Random _rng;


        public void Init(int seed)
        {
            _rng = new Random(seed);
            Initialize();
        }
        private void Initialize()
        {
            if (_itemData == null)
            {
                Debug.LogError("TrackManager: Missing SOItem reference.");
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
            var generatedObstacles = new List<GeneratedItemInfo>();
            int numOfAreaGroupWithObstacle = 0;

            for (int areaGroupIndex = 0; areaGroupIndex < _itemData.NumberOfAreaGroupPerChunk; areaGroupIndex++, numOfAreaGroupWithObstacle++)
            {
                if (_rng.NextDouble() < _itemData.ChanceToSpawnObstaclePerAreaGroup && numOfAreaGroupWithObstacle < _itemData.MaxAreaGroupsWithObstaclePerRoadChunk)
                {
                    if (_rng.NextDouble() < _itemData.ChanceToSpawnTwoObstaclesInAreaGroup)
                    {
                        generatedObstacles.Add(new GeneratedItemInfo(_rng.Next(0, _itemData.ObstacleItems.Length), ItemType.OBSTACLE, areaGroupIndex, 0));
                        generatedObstacles.Add(new GeneratedItemInfo(_rng.Next(0, _itemData.ObstacleItems.Length), ItemType.OBSTACLE, areaGroupIndex, _itemData.NumberOfItemSpawnAreaPerAreaGroup - 1));
                    }
                    else
                    {
                        generatedObstacles.Add(new GeneratedItemInfo(_rng.Next(0, _itemData.ObstacleItems.Length), ItemType.OBSTACLE, areaGroupIndex, _rng.Next(0, _itemData.NumberOfItemSpawnAreaPerAreaGroup)));
                    }
                }
            }

            return generatedObstacles;
        }

        private List<GeneratedItemInfo> GeneratePowerUp(RoadChunkPosition position)
        {
            var generatedPowerUps = new List<GeneratedItemInfo>();
            for (int areaGroupIndex = 0; areaGroupIndex < _itemData.NumberOfAreaGroupPerChunk; areaGroupIndex++)
            {
                if (_rng.NextDouble() < _itemData.ChanceToSpawnPowerUpPerAreaGroup)
                {
                    if (position == RoadChunkPosition.LEFT && _numOfPowerUpSpawnedInLeftSplit < _itemData.MaxNumberOfPowerUpPerSplit)
                    {
                        generatedPowerUps.Add(new GeneratedItemInfo(_rng.Next(0, _itemData.PowerUpItems.Length), ItemType.POWER_UP, areaGroupIndex));
                        _numOfPowerUpSpawnedInLeftSplit++;
                    }
                    else if (position == RoadChunkPosition.RIGHT && _numOfPowerUpSpawnedInRightSplit < _itemData.MaxNumberOfPowerUpPerSplit)
                    {
                        generatedPowerUps.Add(new GeneratedItemInfo(_rng.Next(0, _itemData.PowerUpItems.Length), ItemType.POWER_UP, areaGroupIndex));
                        _numOfPowerUpSpawnedInRightSplit++;
                    }
                }
            }
            return generatedPowerUps;
        }
    }
}
