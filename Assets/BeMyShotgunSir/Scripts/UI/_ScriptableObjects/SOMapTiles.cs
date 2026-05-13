using BeMyShotgunSir.Scripts.UI;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "SOMapTiles", menuName = "Be My Shotgun, Sir!/UI/MapTiles")]
public class SOMapTiles : ScriptableObject
{
    [System.Serializable]
    public struct MapTileData
    {
        public int TurnWeight;
        public RoadChunkTile TileData;
    }

    [SerializeField] private MapTileData[] _mapTiles;

    public RoadChunkTile GetTile(int turnWeight)
    {
        foreach (MapTileData data in _mapTiles)
        {
            if (data.TurnWeight == turnWeight)
            {
                return data.TileData;
            }
        }
        Log.ELazy(() => $"Tile for TurnWeight {turnWeight} not found. Returning null.", this);
        return null;
    }
}
