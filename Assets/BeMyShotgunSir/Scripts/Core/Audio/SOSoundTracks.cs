using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Audio
{
    [CreateAssetMenu(fileName = "_SoundTracks_", menuName = "Be My Shotgun, Sir!/Audio/_SoundTracks_", order = 1)]
    public class SOSoundTracks : ScriptableObject
    {
        [SerializeField] private AudioClip _lobbyTrack;
        [SerializeField] private AudioClip _gameTrack;

        public AudioClip LobbyTrack => _lobbyTrack;
        public AudioClip GameTrack => _gameTrack;
    }
}
