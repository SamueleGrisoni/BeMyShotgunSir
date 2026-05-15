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
            else if (targetSource == null)
            {
                finalSource.gameObject.transform.position = request.StartingPosition;
            }
            finalSource.Play();
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
                _mainSource.volume = _soundTracks.InitSceneMusicVolume;
                _mainSource.loop = true;
                _mainSource.Play();
            }
            if (name == SceneName.Lobby)
            {
                _mainSource.clip = _soundTracks.LobbySceneMusic;
                _mainSource.volume = _soundTracks.LobbySceneMusicVolume;
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
                        if (request.Type != RequestEnum.Loading)
                            return;
                        _mainSource.clip = _soundTracks.LoadingMusic;
                        _mainSource.volume = _soundTracks.LoadingMusicVolume;
                        _mainSource.loop = true;
                        _mainSource.Play(); break;
                    case RequestEnum.StartRace:
                        if (request.Type != RequestEnum.StartRace)
                            return;
                        _mainSource.clip = _soundTracks.RaceSceneMusic;
                        _mainSource.volume = _soundTracks.RaceSceneMusicVolume;
                        _mainSource.loop = true;
                        _mainSource.Play(); break;
                    case RequestEnum.RaceFinish:
                        break;
                    case RequestEnum.UIClick:
                        break;
                    case RequestEnum.PlayerJoin:
                        if (request.Type != RequestEnum.PlayerJoin)
                            return;
                        int index = Random.Range(0, _soundTracks.JoinSound.Clips.Length);
                        request.WithMixerGroup(MixerGroupEnum.SFX)
                        .WithClip(_soundTracks.JoinSound.Clips[index].Clip)
                        .WithVolume(_soundTracks.JoinSound.Clips[index].Volume);
                        ExecuteAudioRequest(request, targetSource);
                        break;
                    case RequestEnum.PlayerLeave:
                        if (request.Type != RequestEnum.PlayerLeave)
                            return;
                        request.WithMixerGroup(MixerGroupEnum.SFX)
                        .WithClip(_soundTracks.LeaveSound)
                        .WithVolume(_soundTracks.LeaveSoundVolume);
                        ExecuteAudioRequest(request, targetSource);
                        break;
                    case RequestEnum.Stop:
                        if (request.Type != RequestEnum.Stop)
                            return;
                        if (targetSource != null)
                        {
                            targetSource.Stop();
                            return;
                        }
                        if (_mainSource == null)
                        {
                            Log.ELazy(() => "Cannot stop main audio because MainSource is not assigned.", this);
                            return;
                        }
                        _mainSource.Stop();
                        break;
                    case RequestEnum.PlayerReady:
                        if (request.Type != RequestEnum.PlayerReady)
                            return;
                        request.WithMixerGroup(MixerGroupEnum.SFX)
                    .WithClip(_soundTracks.ReadySound).As2D()
                    .WithVolume(_soundTracks.ReadySoundVolume);
                        ExecuteAudioRequest(request, targetSource);

                        break;
                    case RequestEnum.LetsGo:
                        if (request.Type != RequestEnum.LetsGo)
                            return;
                        request.WithMixerGroup(MixerGroupEnum.SFX)
                        .WithClip(_soundTracks.LetsGoSound).As2D()
                        .WithVolume(_soundTracks.LetsGoSoundVolume);
                        ExecuteAudioRequest(request, targetSource);
                        break;
                    case RequestEnum.OilSlip:

                        if (request.Type != RequestEnum.OilSlip)
                            return;
                        int oilIndex = Random.Range(0, _soundTracks.OilSlipSound.Clips.Length);
                        request.WithMixerGroup(MixerGroupEnum.SFX)
                        .WithClip(_soundTracks.OilSlipSound.Clips[oilIndex].Clip)
                        .WithVolume(_soundTracks.OilSlipSound.Clips[oilIndex].Volume);
                        ExecuteAudioRequest(request, targetSource);
                        break;
                    case RequestEnum.FinishRace:
                        if (request.Type != RequestEnum.FinishRace)
                            return;
                        _mainSource.clip = _soundTracks.FinishRaceMusic;
                        _mainSource.volume = _soundTracks.FinishRaceMusicVolume;
                        _mainSource.loop = false;
                        _mainSource.Play();
                        break;
                    case RequestEnum.Countdown:
                        if (request.Type != RequestEnum.Countdown)
                            return;
                        _mainSource.clip = _soundTracks.Countdown;
                        _mainSource.volume = _soundTracks.CountdownVolume;
                        _mainSource.loop = false;
                        _mainSource.Play();
                        break;
                    default:
                        break;
                }
                return;
            }
            ExecuteAudioRequest(request, targetSource);
        }
    }
}
