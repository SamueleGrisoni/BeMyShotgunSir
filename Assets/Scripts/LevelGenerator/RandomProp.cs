using UnityEngine;

namespace BeMyShotgunSir.LevelGenerator
{
    public class RandomProp : MonoBehaviour
    {
        private int _indexInCurrentTrack;
        public void SetIndexInCurrentTrack(int index) => _indexInCurrentTrack = index;
    }
}
