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

        private void SpawnObstacle(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            Item item = _itemData.ObstacleItems[itemInfo.index];
            PolygonSpawnArea polygonSpawnArea = chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints[itemInfo.spawnPointIndex];
            //Debug.Log("Spawning obstacle: " + item.name + " in area group: " + itemInfo.areaGroupIndex + " spawn point: " + itemInfo.spawnPointIndex);
            Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), polygonSpawnArea.transform.rotation, chunk.transform);
        }

        private void SpawnPowerUp(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            if (_netController == null)
                return;

            //TODO make this logic networket calling _netController.Spawn(itemInstance)
            Item item = _itemData.PowerUpItems[itemInfo.index];
            foreach (PolygonSpawnArea polygonSpawnArea in chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints)
            {
                //Debug.Log("Spawning power-up: " + item.name + " in area group: " + itemInfo.areaGroupIndex);
                Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), polygonSpawnArea.transform.rotation, chunk.transform);
            }
        }
    }
}
