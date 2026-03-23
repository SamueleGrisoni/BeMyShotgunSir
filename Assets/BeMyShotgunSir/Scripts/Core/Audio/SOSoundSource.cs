using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;

namespace BeMyShotgunSir.Scripts.Core.Audio
{
    [System.Serializable]
    public struct AudioClipInfo
    {
        [field: SerializeField] public AudioClip Clip { get; private set; }
        [field: SerializeField, Range(0, 1)] public float Volume { get; private set; }
        public AudioClipInfo(AudioClip clip, float volume)
        {
            Clip = clip;
            Volume = volume;
        }
    }
    [CreateAssetMenu(fileName = "SoundSourceSO", menuName = "Be My Shotgun, Sir!/Audio/SoundSource")]
    public class SOSoundSource : ScriptableObject
    {
        [field: SerializeField] public MixerGroupEnum MixerGroup { get; private set; }
        [field: SerializeField] public AudioClipInfo[] Clips { get; private set; }
    }
}
