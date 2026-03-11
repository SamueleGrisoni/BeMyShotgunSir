using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track
{
    public class RandomProp : MonoBehaviour
    {
        private int _indexInCurrentTrack;
        public void SetIndexInCurrentTrack(int index) => _indexInCurrentTrack = index;
    }
}
