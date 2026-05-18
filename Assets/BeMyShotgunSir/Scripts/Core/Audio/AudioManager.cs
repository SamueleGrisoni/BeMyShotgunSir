using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using BeMyShotgunSir.Scripts.Events;
using BeMyShotgunSir.Scripts.Utils;
using System.Collections;


namespace BeMyShotgunSir.Scripts.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [field: Header("Music")]
        [field: SerializeField] public EventReference InitSceneMusic { get; private set; }
        [field: SerializeField] public EventReference LobbySceneMusic { get; private set; }
        [field: SerializeField] public EventReference LoadingMusic { get; private set; }
        [field: SerializeField] public EventReference CountdownSound { get; private set; }
        [field: SerializeField] public EventReference StartRaceMusic { get; private set; }
        [field: SerializeField] public EventReference FinishRaceMusic { get; private set; }

        private SOAudioRequestEvent _audioRequestEvent;


        private EventInstance _currentBGM;
        private bool _isInitialized = false;

        private void Awake() => FishNetSceneAdapter.OnSceneInitialized += HandleSceneInitialized;

        public void Initialize()
        {
            if (_isInitialized)
                return;
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;
            SubscribeToAudioRequests();
            _isInitialized = true;
        }

        private void OnDisable() =>
            UnsubscribeFromAudioRequests();

        private void OnDestroy()
        {
            FishNetSceneAdapter.OnSceneInitialized -= HandleSceneInitialized;
            StopCurrentBGM();
        }

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

        private void HandleSceneInitialized(SceneName name)
        {
            switch (name)
            {
                case SceneName.Init:
                    PlayBGM(InitSceneMusic);
                    break;
                case SceneName.Lobby:
                    PlayBGM(LobbySceneMusic);
                    break;
                case SceneName.Persistent:
                    break;
                case SceneName.Race:
                    break;
                default:
                    break;
            }
        }

        private void PlayBGM(EventReference musicEventReference)
        {
            if (musicEventReference.IsNull) return;
            StopCurrentBGM();
            _currentBGM = RuntimeManager.CreateInstance(musicEventReference);
            _currentBGM.start();
        }

        public void StopCurrentBGM()
        {
            if (_currentBGM.isValid())
            {
                _currentBGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _currentBGM.release();
            }
        }

        public void ExecuteAudioRequestHandler(IEventSender sender, AudioRequest request, AudioSource targetSource = null)
        {
            if (request != null)
            {
                Log.DLazy(() => $"Received audio request of type {request.Type} from sender {sender}.", this);
                switch (request.Type)
                {
                    case RequestEnum.Stop:
                        if (request.Type != RequestEnum.Stop)
                            return;
                        break;
                    case RequestEnum.Loading:
                        if (request.Type != RequestEnum.Loading)
                            return;
                        StopCurrentBGM();
                        PlayBGM(LoadingMusic);
                        break;
                    case RequestEnum.Countdown:
                        if (request.Type != RequestEnum.Countdown)
                            return;
                        StopCurrentBGM();
                        if (CountdownSound.IsNull) return;
                        StartCoroutine(PlayAndTrackCountdown(CountdownSound, request.StartingPosition));
                        break;
                    case RequestEnum.StartRace:
                        if (request.Type != RequestEnum.StartRace)
                            return;
                        StopCurrentBGM();
                        PlayBGM(StartRaceMusic);
                        break;
                    case RequestEnum.RaceFinish:
                        StopCurrentBGM();
                        PlayBGM(FinishRaceMusic);
                        break;
                    case RequestEnum.FinishRace:
                        if (request.Type != RequestEnum.FinishRace)
                            return;
                        break;
                    default:
                        break;
                }
                return;
            }
        }

        private IEnumerator PlayAndTrackCountdown(EventReference eventRef, Vector3 position)
        {
            EventInstance countdownInstance = RuntimeManager.CreateInstance(eventRef);
            countdownInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            countdownInstance.start();
            PLAYBACK_STATE playbackState;
            countdownInstance.getPlaybackState(out playbackState);

            while (playbackState != PLAYBACK_STATE.STOPPED)
            {
                yield return null;
                countdownInstance.getPlaybackState(out playbackState);
            }
            countdownInstance.release();
        }
    }
}
