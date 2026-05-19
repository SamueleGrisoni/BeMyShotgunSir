using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Items
{
    [RequireComponent(typeof(Collider))]
    public class TriggerRelay : MonoBehaviour
    {
        private GenericPowerUp _owner;

        public void Init(GenericPowerUp owner)
        {
            _owner = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            _owner?.OnChildTriggerEnter(other);
        }
    }
}
