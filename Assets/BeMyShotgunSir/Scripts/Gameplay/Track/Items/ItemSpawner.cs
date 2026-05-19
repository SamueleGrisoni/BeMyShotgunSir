using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public class ItemSpawner : NetworkBehaviour
    {
        [SerializeField] private SOItem _itemData;

        public void PopulateChunkWithItems(RoadChunk chunk, List<GeneratedItemInfo> items)
        {
            foreach (GeneratedItemInfo itemInfo in items)
            {
                if (itemInfo.type == ItemType.OBSTACLE)
                    SpawnObstacle(chunk, itemInfo);
                else
                    SpawnPowerUp(chunk, itemInfo);
            }
        }

        public void ClearItemsFromChunk(RoadChunk chunk)
        {
            foreach (ItemAreaGroup areaGroup in chunk.AreaGroup)
            {
                foreach (PolygonSpawnArea spawnPoint in areaGroup.ItemSpawnPoints)
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
            ObstacleSpawner item = _itemData.ObstacleItems[itemInfo.index];
            PolygonSpawnArea polygonSpawnArea = chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints[itemInfo.spawnPointIndex];
            Instantiate(item, polygonSpawnArea.GetPolygonBoundsCenter(), polygonSpawnArea.transform.rotation, polygonSpawnArea.transform);
        }

        private void SpawnPowerUp(RoadChunk chunk, GeneratedItemInfo itemInfo)
        {
            PowerUp powerUpType = _itemData.PowerUpType[itemInfo.index];
            Log.ELazy(() => $"Spawning power-up of type {powerUpType} in chunk {chunk.ChunkNumber} at area group {itemInfo.areaGroupIndex}", this);
            foreach (PolygonSpawnArea polygonSpawnArea in chunk.AreaGroup[itemInfo.areaGroupIndex].ItemSpawnPoints)
            {
                GenericPowerUp nob = Instantiate(_itemData.GenericPowerUpPrefab, polygonSpawnArea.GetPolygonBoundsCenter(), polygonSpawnArea.transform.rotation);
                nob.SetChunkIndex(chunk.ChunkNumber);
                nob.SetPowerUpType(powerUpType);
                Spawn(nob);
            }
        }
    }
}
