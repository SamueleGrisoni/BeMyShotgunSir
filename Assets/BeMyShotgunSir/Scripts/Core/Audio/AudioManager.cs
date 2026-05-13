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

            FishNetSceneAdapter.OnSceneInitialized += HandleSceneInitialized;
        }

        private void Start()
        {
            Initialize();
        }

        [SerializeField] private AudioPooler _pooler;
        [SerializeField] private SOSoundTracks _soundTracks;
        private AudioSource _mainSource;
        private SOAudioRequestEvent _audioRequestEvent;

        private bool _isInitialized = false;
        public void Initialize()
        {
            if (_isInitialized)
                return;

            Debug.Assert(_pooler != null, "AudioManager: AudioPooler reference is not assigned in the inspector.", this);
            Debug.Assert(_soundTracks != null, "AudioManager: SOSoundTracks reference is not assigned in the inspector.", this);
            TryGetComponent(out _mainSource);

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
            if (_audioRequestEvent == null)
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

        public void ExecuteAudioRequest(AudioRequest request, AudioSource targetSource = null, bool isMain = false)
        {
            if (request == null)
            {
                Log.ELazy(() => "Cannot execute audio request because the request is null.", this);
                return;
            }
            if (request.StopMain)
            {
                if (_mainSource == null)
                {
                    Log.ELazy(() => "Cannot stop main audio because MainSource is not assigned.", this);
                    return;
                }
                _mainSource.Stop();
                return;
            }

            if (request.Clip == null)
            {
                Log.ELazy(() => "Cannot execute audio request because clip is null.", this);
                return;
            }

            bool isPooled = (targetSource == null);
            if (isPooled && _pooler == null)
            {
                Log.ELazy(() => "Pooler is not assigned!", this);
                return;
            }
            AudioSource finalSource = (isPooled) ? _pooler.GetAudioSource() : request.AsMain ? _mainSource : targetSource;
            if (finalSource == null)
            {
                Log.ELazy(() => "Failed to obtain an AudioSource for the request.", this);
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

        private void HandleSceneInitialized(SceneName name)
        {
            if (name == SceneName.Init)
            {
                _mainSource.clip = _soundTracks.InitSceneMusic;
                _mainSource.loop = true;
                _mainSource.Play();
            }
            if (name == SceneName.Lobby)
            {
                _mainSource.clip = _soundTracks.LobbySceneMusic;
                _mainSource.loop = true;
                _mainSource.Play();
            }
            // if (name == SceneName.Race)
            // {
            //     _mainSource.clip = _soundTracks.RaceSceneMusic;
            //     _mainSource.loop = true;
            //     _mainSource.Play();
            // }
        }

        public void ExecuteAudioRequestHandler(IEventSender sender, AudioRequest request, AudioSource targetSource = null)
        {
            if (request != null)
            {
                Log.DLazy(() => $"Received audio request of type {request.Type} from sender {sender}.", this);
                switch (request.Type)
                {
                    case RequestEnum.Play:
                        ExecuteAudioRequest(request, targetSource);
                        break;
                    case RequestEnum.Loading:
                        HandleLoadingMusicRequest(sender, request);
                        break;
                    case RequestEnum.StartRace:
                        HandleStartRaceMusicRequest(sender, request);
                        break;
                    case RequestEnum.RaceFinish:
                        break;
                    case RequestEnum.UIClick:
                        break;
                    default:
                        break;
                }
                return;
            }
            ExecuteAudioRequest(request, targetSource);
        }

        public void HandleLoadingMusicRequest(IEventSender sender, AudioRequest request)
        {
            if (request.Type != RequestEnum.Loading)
                return;
            _mainSource.clip = _soundTracks.LoadingMusic;
            _mainSource.loop = true;
            _mainSource.Play();
        }

        public void HandleStartRaceMusicRequest(IEventSender sender, AudioRequest request)
        {
            if (request.Type != RequestEnum.StartRace)
                return;
            _mainSource.clip = _soundTracks.RaceSceneMusic;
            _mainSource.loop = true;
            _mainSource.Play();
        }
    }
}
