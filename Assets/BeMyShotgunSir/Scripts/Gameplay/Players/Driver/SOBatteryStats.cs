using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    [CreateAssetMenu(fileName = "SOBatteryStats", menuName = "Scriptable Objects/BatteryStats")]
    public class SOBatteryStats : ScriptableObject
    {
        [field: SerializeField] public float MaxBatteryCharge { get; private set; }
        [field: SerializeField] public float ChargeBatteryTimeRate { get; private set; }
        [field: SerializeField] public float ChargeBatterAmountRate { get; private set; }
        [field: SerializeField] public float ConsumeBatteryTimeRate { get; private set; }
        [field: SerializeField] public float ConsumeBatteryAmountRate { get; private set; }
    }
}
