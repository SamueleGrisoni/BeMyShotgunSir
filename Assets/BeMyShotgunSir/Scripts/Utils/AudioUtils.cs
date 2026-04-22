
using BeMyShotgunSir.Scripts.Core.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace BeMyShotgunSir.Scripts.Utils
{
    public enum MixerGroupEnum
    {
        Master,
        Music,
        SFX,
        Voice,
        Ambience
    }
    public class AudioUtils
    {
        private static AudioMixer _mixer;
        public static AudioMixer Mixer
        {
            get => _mixer;
            set
            {
                if (value == null)
                    return;

                if (_mixer != null && _mixer != value)
                    Log.WLazy(() => "AudioUtils: Replacing previously assigned mixer instance.", typeof(AudioUtils));

                _mixer = value;
            }
        }
        public static AudioMixerGroup GetMixerGroup(MixerGroupEnum group)
        {
            if (_mixer == null)
            {
                Log.ELazy(() => "AudioUtils: Mixer is not assigned! Returning null.", typeof(AudioUtils));
                return null;
            }

            AudioMixerGroup[] groups = _mixer.FindMatchingGroups(GetGroupString(group));

            if (groups == null || groups.Length == 0)
            {
                Log.ELazy(() => $"AudioUtils: No MixerGroup found for {group}.", typeof(AudioUtils));
                return null;
            }

            return groups[0];
        }

        public static string GetGroupString(MixerGroupEnum group)
        {
            switch (group)
            {
                case MixerGroupEnum.Master:
                    return "Master";
                case MixerGroupEnum.Music:
                    return "BackgroundMusic";
                case MixerGroupEnum.SFX:
                    return "SFX";
                case MixerGroupEnum.Voice:
                    return "Voice";
                case MixerGroupEnum.Ambience:
                    return "Ambience";
                default:
                    return "Master";
            }
        }

        /// <summary>
        /// Gets a random AudioClip from the provided array.
        /// If the array is null or empty, returns null
        /// </summary>
        public static AudioClip GetRandomClip(AudioClip[] audioClips)
        {
            if (audioClips == null || audioClips.Length == 0)
                return null;

            int index = Random.Range(0, audioClips.Length);
            return audioClips[index];
        }

        /// <summary>
        /// Gets a random AudioClip from the provided AudioClipInfo array.
        /// If the array is null or empty, returns null
        /// </summary>
        public static AudioClip GetRandomClip(AudioClipInfo[] audioClipInfos)
        {
            if (audioClipInfos == null || audioClipInfos.Length == 0)
                return null;

            int index = Random.Range(0, audioClipInfos.Length);
            return audioClipInfos[index].Clip;
        }

        public static int GetDeterministicHash(string input)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in input)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }
    }
}
