using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    public struct GeneratedItemInfo
    {
        public int index;
        public ItemType type;
        public int areGroupIndex;

        public GeneratedItemInfo(int index, ItemType type, int areGroupIndex)
        {
            this.index = index;
            this.type = type;
            this.areGroupIndex = areGroupIndex;
        }
    }

    public class ItemGenerator : MonoBehaviour
    {
        [SerializeField] private TrackSeed _trackSeed;
        [SerializeField] private SOItem _itemData;

        [SerializeField] private List<Item> _obstacleItems;
        [SerializeField] private List<Item> _powerUpItems;

        private Random Rng => _trackSeed.Rng;

        private void Start()
        {
            if (_trackSeed == null || _itemData == null)
            {
                Debug.LogError("TrackManager: Missing TrackSeed or SOItem reference.");
                return;
            }
        }

        public List<GeneratedItemInfo> GenerateItemsForRoadChunk(GeneratedRoadChunkInfo roadChunkInfo)
        {
            return roadChunkInfo.position == RoadChunkPosition.MIDDLE ? GenerateObstacle(roadChunkInfo) : GeneratePowerUp(roadChunkInfo);
        }

        private List<GeneratedItemInfo> GenerateObstacle(GeneratedRoadChunkInfo roadChunkInfo)
        {
            return new List<GeneratedItemInfo>();
        }

        private List<GeneratedItemInfo> GeneratePowerUp(GeneratedRoadChunkInfo roadChunkInfo)
        {
            return new List<GeneratedItemInfo>();
        }
    }
}
