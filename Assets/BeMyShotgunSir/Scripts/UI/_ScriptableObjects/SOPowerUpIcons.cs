using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "SOPowerUpIcons", menuName = "Be My Shotgun, Sir!/UI/PowerUpIcons")]
public class SOPowerUpIcons : ScriptableObject
{
    [System.Serializable]
    public struct PowerUpIconData
    {
        public PowerUp Type;
        public Sprite Icon;
    }

    [SerializeField] private PowerUpIconData[] _powerUpIcons;

    public Sprite GetIcon(PowerUp powerUp)
    {
        foreach (PowerUpIconData data in _powerUpIcons)
        {
            if (data.Type == powerUp)
            {
                return data.Icon;
            }
        }
        Log.ELazy(() => $"Icon for PowerUp {powerUp} not found. Returning null.", this);
        return null;
    }
}
