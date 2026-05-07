using System.Collections.Generic;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOPowerUpsData", menuName = "Be My Shotgun, Sir!/PowerUpsData", order = 0)]
    public class SOPowerUpsData : ScriptableObject
    {
        private Dictionary<PowerUp, SOPowerUp> _powerUpDefinitions;
        [field: SerializeField] public SOPowerUp[] PowerUps { get; private set; }

        public Dictionary<PowerUp, SOPowerUp> GetPowerUpDefinitions()
        {
            if (_powerUpDefinitions != null) return _powerUpDefinitions;

            _powerUpDefinitions = new Dictionary<PowerUp, SOPowerUp>();
            foreach (SOPowerUp powerUp in PowerUps)
            {
                if (powerUp != null)
                {
                    _powerUpDefinitions[powerUp.PowerUpType] = powerUp;
                }
            }

            return _powerUpDefinitions;
        }
    }
}
