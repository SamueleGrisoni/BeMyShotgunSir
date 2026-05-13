using System;
using UnityEngine;
namespace BeMyShotgunSir.Scripts.UI
{
    [Serializable]
    public class RoadChunkTile : MonoBehaviour
    {
        [SerializeField] private int _anchorInPx;
        [SerializeField] private int _anchorOutPx;
        [SerializeField] private Sprite _icon;
        [SerializeField] private int _turnWeight;

        public int AnchorInPx => _anchorInPx;
        public int AnchorOutPx => _anchorOutPx;
        public Sprite Icon => _icon;
        public int TurnWeight => _turnWeight;

    }
}
