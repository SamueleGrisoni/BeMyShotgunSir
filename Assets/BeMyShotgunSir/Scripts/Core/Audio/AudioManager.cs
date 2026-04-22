using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.Audio;
using static BeMyShotgunSir.Scripts.Utils.AudioUtils;

namespace BeMyShotgunSir.Scripts.Core.Audio
{
    [RequireComponent(typeof(AudioPooler))]
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        private void Awake()
        {
            Mixer = Resources.Load<AudioMixer>("BMSS-AudioMixer");
            Debug.Assert(Mixer != null, "AudioManager: Failed to load AudioMixer from Resources.", this);
        }

        [SerializeField] private AudioPooler _pooler;
        [SerializeField] private SOSoundTracks _soundTracks;
        private AudioSource _musicSource;
        private SOAudioRequestEvent _audioRequestEvent;

        private bool _isInitialized = false;
        public void Initialize()
        {
            if (_isInitialized)
                return;

            Debug.Assert(_pooler != null, "AudioManager: AudioPooler reference is not assigned in the inspector.", this);
            Debug.Assert(_soundTracks != null, "AudioManager: SOSoundTracks reference is not assigned in the inspector.", this);
            TryGetComponent(out _musicSource);
            ExecuteAudioRequest(new AudioRequest(_soundTracks.LobbyTrack, 1f).As2D().Looping(), _musicSource);

            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            SubscribeToAudioRequests();

            _isInitialized = true;
        }

        private void OnEnable() =>
            SubscribeToAudioRequests();

        private void OnDisable() =>
            UnsubscribeFromAudioRequests();

        private void OnDestroy() =>
            UnsubscribeFromAudioRequests();

        private void SubscribeToAudioRequests()
        {
            if (!_isInitialized || _audioRequestEvent == null)
                return;

            _audioRequestEvent.OnEventRaised -= ExecuteAudioRequestHandler;
            _audioRequestEvent.OnEventRaised += ExecuteAudioRequestHandler;
        }

        private void UnsubscribeFromAudioRequests()
        {
            if (_audioRequestEvent == null)
                return;

            _audioRequestEvent.OnEventRaised -= ExecuteAudioRequestHandler;
        }

        public void ExecuteAudioRequestHandler(IEventSender sender, AudioRequest request, AudioSource targetSource = null) =>
        ExecuteAudioRequest(request, targetSource);

        public void ExecuteAudioRequest(AudioRequest request, AudioSource targetSource = null)
        {
            if (request == null)
            {
                Log.ELazy(() => "AudioManager: Cannot execute audio request because the request is null.", this);
                return;
            }

            if (request.Clip == null)
            {
                Log.ELazy(() => "AudioManager: Cannot execute audio request because clip is null.", this);
                return;
            }

            bool isPooled = (targetSource == null);
            if (isPooled && _pooler == null)
            {
                Log.ELazy(() => "AudioManager: Pooler is not assigned!", this);
                return;
            }
            AudioSource finalSource = (isPooled) ? _pooler.GetAudioSource() : targetSource;
            if (finalSource == null)
            {
                Log.ELazy(() => "AudioManager: Failed to obtain an AudioSource for the request.", this);
                return;
            }
            ApplyRequestToSource(request, finalSource);

            if (isPooled)
            {
                finalSource.gameObject.SetActive(true);
                finalSource.Play();

                if (finalSource.TryGetComponent(out PooledAudioSource pooled))
                {
                    if (request.Loop)
                        pooled.StartMonitoring();
                    else
                        pooled.ReturnToPool(request.Clip.length);
                }
            }
            else
            {
                finalSource.gameObject.transform.position = request.StartingPosition;
                finalSource.Play();
            }
        }

        private void ApplyRequestToSource(AudioRequest request, AudioSource finalSource)
        {
            finalSource.outputAudioMixerGroup = GetMixerGroup(request.MixerGroup);
            finalSource.clip = request.Clip;
            finalSource.volume = request.Volume;
            finalSource.pitch = request.Pitch;
            finalSource.spatialBlend = request.SpatialBlend;
            finalSource.loop = request.Loop;
            request.BindSource(finalSource);
        }
    }
}
