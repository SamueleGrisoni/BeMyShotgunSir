using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    [CreateAssetMenu(fileName = "SOSidecarStats", menuName = "Scriptable Objects/SOSidecarStats")]
    public class SOSidecarStats : ScriptableObject
    {
        [field: SerializeField] public float AccelerationForce { get; private set; }
        [field: SerializeField] public float MaxSpeed { get; private set; }
        [field: SerializeField] public float MaxSpeedWithBoost { get; private set; }
        [field: SerializeField] public float BoostDuration { get; private set; }
        [field: SerializeField] public float SteeringForce { get; private set; }
        [field: SerializeField] public float Gravity { get; private set; }
        [field: SerializeField] public float LateralGripFactor { get; private set; }
        [field: SerializeField] public float SteerAngularRotation { get; private set; }
        [field: SerializeField] public float SteerAngularRotationSlerp { get; private set; }
        [field: SerializeField] public float GroundAllignmentSpeed { get; private set; }
        [Header("Boost Settings")]
        [field: SerializeField] public float MaxBatteryCharge { get; private set; }
        [field: SerializeField] public float ChargeBatteryTimeRate { get; private set; }
        [field: SerializeField] public float ChargeBatterAmountRate { get; private set; }
        [field: SerializeField] public float ConsumeBatteryTimeRate { get; private set; }
        [field: SerializeField] public float ConsumeBatteryAmountRate { get; private set; }
        [field: SerializeField] public float BoostForce { get; private set; }
    }
}
