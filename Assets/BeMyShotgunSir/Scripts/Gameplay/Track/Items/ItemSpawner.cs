using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private SOItem _itemData;
        private NetworkBehaviour _netController; //DANGER

        public void PopulateChunkWithItems(RoadChunk chunk, List<GeneratedItemInfo> items)
        {
            foreach (GeneratedItemInfo itemInfo in items)
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

        public void ClearItemsFromChunk(RoadChunk chunk)
        {
            foreach (var areaGroup in chunk.AreaGroup)
            {
                foreach (var spawnPoint in areaGroup.ItemSpawnPoints)
                {
                    foreach (Transform child in spawnPoint.transform)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        private void SpawnObstacle(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            var item = _itemData.ObstacleItems[itemInfo.index];
            var polygonSpawnArea = chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints[itemInfo.spawnPointIndex];
            //Debug.Log("Spawning obstacle: " + item.name + " in area group: " + itemInfo.areaGroupIndex + " spawn point: " + itemInfo.spawnPointIndex);
            Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), polygonSpawnArea.transform.rotation, polygonSpawnArea.transform);
        }

        private void SpawnPowerUp(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            var item = _itemData.PowerUpItems[itemInfo.index];
            foreach (var polygonSpawnArea in chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints)
            {
                //Debug.Log("Spawning power-up: " + item.name + " in area group: " + itemInfo.areaGroupIndex);
                Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), polygonSpawnArea.transform.rotation, polygonSpawnArea.transform);
            }
        }
    }
}
