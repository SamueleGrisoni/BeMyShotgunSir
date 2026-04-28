using BeMyShotgunSir.Scripts.Core.Audio;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    [CreateAssetMenu(fileName = "SORaceSounds", menuName = "Be My Shotgun, Sir!/Audio/SORaceSounds", order = 0)]
    public class SORaceSounds : SOSounds
    {
        [field: SerializeField] public AudioClip StartRaceSound { get; private set; }
        [field: SerializeField] public AudioClip CountDownSound { get; private set; }
    }
}
