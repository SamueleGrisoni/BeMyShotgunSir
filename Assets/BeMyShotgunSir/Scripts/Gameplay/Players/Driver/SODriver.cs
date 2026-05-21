using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    [CreateAssetMenu(fileName = "SODriver", menuName = "Be My Shotgun, Sir!/Driver/SODriver", order = 0)]
    public class SODriver : ScriptableObject
    {
        [field: SerializeField] public DriverController[] DriverPrefabs { get; private set; }
    }
}
