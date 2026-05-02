using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public enum PowerUpType
    {
        Shield,
        Armor,
        Invisibility,
        // SwapRole,
        // ClimateChange,
        Spear,
        StealPowerUp,
        RerollPowerUp,
        RoadBlock,
        // Lasso
    }

    public class PowerUp : MonoBehaviour
    {
        public PowerUpType Type { get; private set; }
    }
}
