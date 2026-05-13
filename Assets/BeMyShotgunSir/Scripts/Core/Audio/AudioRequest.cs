using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;


namespace BeMyShotgunSir.Scripts.Core.Audio
{
    public enum RequestEnum
    {
        Play = 0,
        Loading,
        StartRace,
        RaceFinish,
        // ui
        UIClick,
    }

    public class AudioRequest
    {
        public RequestEnum Type { get; private set; } = RequestEnum.Play;
        public AudioClip Clip { get; private set; }
        public bool AsMain { get; private set; } = false;
        public bool StopMain { get; private set; } = false;
        public Vector3 StartingPosition { get; private set; }
        public MixerGroupEnum MixerGroup { get; private set; } = MixerGroupEnum.Master;
        public float Volume { get; private set; } = 1f;
        public float Pitch { get; private set; } = 1f;
        public float SpatialBlend { get; private set; } = 1f; // Default to 3D sound
        public bool Loop { get; private set; } = false;
        private AudioSource _boundSource; //if the request is played in a source, this will hold a reference to it for runtime updates

        public AudioRequest(RequestEnum type)
        {
            Type = type;
        }

        public AudioRequest(SOSoundSource soundSource, Vector3 startingPosition = default)
        {
            if (soundSource == null || soundSource.Clips == null || soundSource.Clips.Length == 0)
            {
                Log.ELazy(() => "SOSoundSource is null or has no clips. Using safe defaults.", this);
                StartingPosition = startingPosition;
                return;
            }

            int randomIndex = Random.Range(0, soundSource.Clips.Length);
            AudioClipInfo clipInfo = soundSource.Clips[randomIndex];

            Clip = clipInfo.Clip;
            Volume = clipInfo.Volume;
            StartingPosition = startingPosition;
            MixerGroup = soundSource.MixerGroup;
        }

        public AudioRequest(AudioClip clip, float volume = 1f, Vector3 startingPosition = default)
        {
            Clip = clip;
            Volume = volume;
            StartingPosition = startingPosition;
        }

        public AudioRequest WithVolume(float volume) { Volume = volume; return this; }
        public AudioRequest WithMixerGroup(MixerGroupEnum mixerGroup) { MixerGroup = mixerGroup; return this; }
        public AudioRequest WithPitch(float pitch) { Pitch = pitch; return this; }
        public AudioRequest As2D() { SpatialBlend = 0f; return this; }
        public AudioRequest As3D() { SpatialBlend = 1f; return this; }
        public AudioRequest Looping(bool loop = true) { Loop = loop; return this; }
        public AudioRequest AsMainSource(bool asMain = true) { AsMain = asMain; return this; }
        public void BindSource(AudioSource source)
        {
            if (_boundSource == null)
                _boundSource = source;
        }

        public void Stop()
        {
            if (_boundSource == null)
            {
                Log.ELazy(() => "Cannot stop audio because the request is not bound to any AudioSource.", this);
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
