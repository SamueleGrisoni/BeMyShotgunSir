using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;


namespace BeMyShotgunSir.Scripts.Core.Audio
{
    public class AudioRequest
    {
        private static AudioManager _manager;

        public AudioClip Clip { get; private set; }
        public Vector3 StartingPosition { get; private set; }
        public MixerGroupEnum MixerGroup { get; private set; } = MixerGroupEnum.Master;
        public float Volume { get; private set; } = 1f;
        public float Pitch { get; private set; } = 1f;
        public float SpatialBlend { get; private set; } = 1f; // Default to 3D sound
        public bool Loop { get; private set; } = false;
        private AudioSource _targetSource; //if null the AudioManager will play the clip at the given position
        private AudioSource _boundSource; //if the request is played in a source, this will hold a reference to it for runtime updates

        public AudioRequest(SOSoundSource soundSource, Vector3 startingPosition = default)
        {
            Clip = AudioUtils.GetRandomClip(soundSource.Clips);
            Volume = soundSource.Clips[0].Volume;
            StartingPosition = startingPosition;
            MixerGroup = soundSource.MixerGroup;
            Initialize();
        }

        public AudioRequest(AudioClip clip, float volume = 1f, Vector3 startingPosition = default)
        {
            Clip = clip;
            Volume = volume;
            StartingPosition = startingPosition;
            Initialize();
        }

        private static void Initialize()
        {
            if (_manager != null) return;
            _manager = GameServices.Instance.AudioManager;
        }

        public AudioRequest WithVolume(float volume) { Volume = volume; return this; }
        public AudioRequest WithMixerGroup(MixerGroupEnum mixerGroup) { MixerGroup = mixerGroup; return this; }
        public AudioRequest WithPitch(float pitch) { Pitch = pitch; return this; }
        public AudioRequest As2D() { SpatialBlend = 0f; return this; }
        public AudioRequest As3D() { SpatialBlend = 1f; return this; }
        public AudioRequest Looping(bool loop = true) { Loop = loop; return this; }
        public void BindSource(AudioSource source)
        {
            if (_boundSource == null)
                _boundSource = source;
        }

        public void Play()
        {
            if (_manager == null)
            {
                Log.ELazy(() => "AudioRequest: Cannot play audio because the AudioManager is null.", this);
                return;
            }
            _manager.ExecuteAudioRequest(this, _targetSource);
        }

        public void Stop()
        {
            if (_boundSource == null)
            {
                Log.ELazy(() => "AudioRequest: Cannot stop audio because the request is not bound to any AudioSource.", this);
                return;
            }
            _boundSource.Stop();
        }
        public void UpdatePitch(float newPitch)
        {
            Pitch = newPitch;
            if (_boundSource != null)
                _boundSource.pitch = newPitch;
        }

    }
}
