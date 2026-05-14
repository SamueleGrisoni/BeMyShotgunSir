using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Audio
{
    [CreateAssetMenu(fileName = "SOSoundTracks", menuName = "Be My Shotgun, Sir!/Audio/SOSoundTracks", order = 1)]
    public class SOSoundTracks : ScriptableObject
    {
        [field: SerializeField] public AudioClip InitSceneMusic { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float InitSceneMusicVolume { get; private set; } = 0.5f;
        [field: SerializeField] public AudioClip LoadingMusic { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float LoadingMusicVolume { get; private set; } = 0.5f;
        [field: SerializeField] public AudioClip LobbySceneMusic { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float LobbySceneMusicVolume { get; private set; } = 0.5f;
        [field: SerializeField] public AudioClip RaceSceneMusic { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float RaceSceneMusicVolume { get; private set; } = 0.5f;

        [field: SerializeField] public SOSoundSource JoinSound { get; private set; }
        [field: SerializeField] public AudioClip LeaveSound { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float LeaveSoundVolume { get; private set; } = 0.5f;
        [field: SerializeField] public AudioClip ReadySound { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float ReadySoundVolume { get; private set; } = 0.5f;
        [field: SerializeField] public AudioClip LetsGoSound { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float LetsGoSoundVolume { get; private set; } = 0.5f;
        [field: SerializeField] public SOSoundSource OilSlipSound { get; private set; }




    }
}
