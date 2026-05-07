using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOShield_PU", menuName = "Be My Shotgun, Sir!/PowerUp/Shield", order = 0)]
    public class SOShield_PU : SOPowerUp
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            //TODO call driver rpc to set shield active on client, and add visual effect
        }
    }
}
