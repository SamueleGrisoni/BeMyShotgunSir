using UnityEngine;

[CreateAssetMenu(fileName = "SOSidecarStats", menuName = "Sidecar/Stats")]
public class SOSidecarStats : ScriptableObject
{
    [field: SerializeField] public float AccelerationForce { get; private set; }
    [field: SerializeField] public float BrakingForce { get; private set; }
    [field: SerializeField] public float SteeringSpeed { get; private set; }
    [field: SerializeField] public AnimationCurve SteeringCurve { get; private set; }
    [field: SerializeField] public float MaxSpeedDefault { get; private set; }
    [field: SerializeField] public float MaxSpeedWithBoost { get; private set; }
    [field: SerializeField] public float GripFactor { get; private set; }
    [field: SerializeField] public float VisualDriftMultiplier { get; private set; }
    [field: SerializeField] public float GroundAlignSpeed { get; private set; }
}
