using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.Players.Driver
{
    public class DriverStats : MonoBehaviour
    {
        [field: Header("Movement Stats")]
        [field: SerializeField] public SOSidecarStats NormalStats { get; private set; }
        [field: SerializeField] public SOSidecarStats BoostStats { get; private set; }
        [field: SerializeField] public SOSidecarStats GrassStats { get; private set; }

        [field: Header("Battery")]
        [field: SerializeField] public SOBatteryStats BatteryStats { get; private set; }

        [field: Header("Animations")]
        [field: SerializeField] public SOSidecarAnimationStats AnimationStats { get; private set; }
    }
}