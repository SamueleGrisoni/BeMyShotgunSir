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
        /*
        // LateralGripFactor serve per rendere più o meno forte l'effetto del grip laterale
        // Con un valore basso il sidecar sbanda verso l'esterno durante una curva
        // Con un valore alto il sidecar sembra andare sui binari nelle curve
        */
        [field: SerializeField] public float LateralGripFactor { get; private set; }
        /*
        // SteerAngularRotation determina di quanti gradi ruota il sidecar rispetto al parent
        */
        [field: SerializeField] public float SteerAngularRotation { get; private set; }
        /*
        // SteerAngularRotationSlerp è in Gradi al secondo.
        // Quindi determina di quanti gradi ogni secondo la velocità della sfera viene allineata alla direzione del sidecar  
        */
        [field: SerializeField] public float SteerAngularRotationSlerp { get; private set; }
        [field: SerializeField] public float GroundAllignmentSpeed { get; private set; }
        [field: SerializeField] public float MaxBatteryCharge { get; private set; }
        [field: SerializeField] public float ChargeBatteryTimeRate { get; private set; }
        [field: SerializeField] public float ChargeBatterAmountRate { get; private set; }
        [field: SerializeField] public float ConsumeBatteryTimeRate { get; private set; }
        [field: SerializeField] public float ConsumeBatteryAmountRate { get; private set; }
        [field: SerializeField] public float BoostForce { get; private set; }
    }
}
