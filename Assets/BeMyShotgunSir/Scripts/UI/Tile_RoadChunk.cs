using UnityEngine;
namespace BeMyShotgunSir.Scripts.UI
{
    public class Tile_RoadChunk : MonoBehaviour
    {

        [SerializeField] private int _anchorInPx;
        [SerializeField] private int _anchorOutPx;
        [SerializeField] private Sprite _icon;

        public int AnchorInPx => _anchorInPx;
        public int AnchorOutPx => _anchorOutPx;
        public Sprite Icon => _icon;

    }
}
