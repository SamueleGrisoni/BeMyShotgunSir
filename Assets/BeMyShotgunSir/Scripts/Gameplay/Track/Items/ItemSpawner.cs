using System.Collections.Generic;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private SOItem _itemData;
        public void PopulateChunkWithItems(RoadChunk chunk, List<GeneratedItemInfo> items)
        {
            foreach (var itemInfo in items)
            {
                if (itemInfo.type == ItemType.OBSTACLE)
                {
                    SpawnObstacle(chunk, itemInfo);
                }
                else
                {
                    SpawnPowerUp(chunk, itemInfo);
                }
            }
        }

        private void SpawnObstacle(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            var item = _itemData.ObstacleItems[itemInfo.index];
            var polygonSpawnArea = chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints[itemInfo.spawnPointIndex];
            Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), Quaternion.identity, chunk.transform);
        }

        private void SpawnPowerUp(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            var item = _itemData.PowerUpItems[itemInfo.index];
            foreach (var polygonSpawnArea in chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints)
            {
                Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), Quaternion.identity, chunk.transform);
            }
        }
    }
}
