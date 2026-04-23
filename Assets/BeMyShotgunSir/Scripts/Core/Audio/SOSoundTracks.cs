using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Audio
{
    [CreateAssetMenu(fileName = "SOSoundTracks", menuName = "Be My Shotgun, Sir!/Audio/SOSoundTracks", order = 1)]
    public class SOSoundTracks : ScriptableObject
    {
        [field: SerializeField] public AudioClip LobbyTrack { get; private set; }
        [field: SerializeField] public AudioClip GameTrack { get; private set; }
    }
}
