using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    [CreateAssetMenu(fileName = "SOSidecarAnimationStats", menuName = "Scriptable Objects/AnimationStats")]
    public class SOSidecarAnimationStats : ScriptableObject
    {
        [field: Header("--- Steer Animation ---")]
        [field: SerializeField] public float MaxSteerAngle { get; private set; }
        [field: SerializeField] public float SteerAnimationSpeed { get; private set; }

        [field: Header("--- Oil Animation ---")]
        [field: SerializeField] public float OilTotalRotation { get; private set; }
        [field: SerializeField] public float OilAnimationDuration { get; private set; }

        [field: Header("--- Bump ---")]
        [field: SerializeField] public float BumpForce { get; private set; }
        [field: SerializeField] public float BumpStunDuration { get; private set; }
        [field: SerializeField] public float BumpRadius { get; private set; }
        [field: SerializeField] public float RotationPerTick { get; private set; }
    }
}
