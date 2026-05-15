using FishNet.Object.Synchronizing;
using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Events;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    [RequireComponent(typeof(RaceNetStateStore))]
    public sealed class RaceClientProjector : MonoBehaviour
    {
        //utility
        private bool _log = true;

        //references
        private IRaceNetStateSubscribe _state;
        private RaceViewModel _viewModel;
        private SOAudioRequestEvent _audioRequestEvent;
        [SerializeField] private SORaceSounds _sounds;

        private void Awake()
        {
            TryGetComponent(out _state);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;

            if (_state == null)
                Log.ELazy(() => "RaceClientProjection requires RaceNetStateStore on the same GameObject.", this);
            if (_sounds == null)
                Log.ELazy(() => $"SORaceSounds reference is not assigned in the inspector.", this);
        }

        private void OnEnable()
        {
            //STATE SYNC CALLBACKS
            if (_state == null)
                return;
            _state.PlayerStates_Sub.OnChange += OnRacePlayerStateChanged;
            _state.TeamData_Sub.OnChange += OnRaceTeamDataChanged;
            _state.Leaderboard_Sub.OnChange += OnLeaderboardChanged;
            _state.Seed_Sub.OnChange += OnSeedChanged;
        }

        public void Init(RaceViewModel viewModel)
        {
            if (_viewModel == null)
                _viewModel = viewModel;
        }

        private void OnDisable()
        {
            //STATE SYNC CALLBACKS
            if (_state == null)
                return;
            _state.PlayerStates_Sub.OnChange -= OnRacePlayerStateChanged;
            _state.TeamData_Sub.OnChange -= OnRaceTeamDataChanged;
            _state.Leaderboard_Sub.OnChange -= OnLeaderboardChanged;
            _state.Seed_Sub.OnChange -= OnSeedChanged;
        }

        private void OnSeedChanged(int? prev, int? next, bool asServer)
        {
            if (asServer)
                return;
        }

        private void OnRacePlayerStateChanged(SyncDictionaryOperation op, int key, RacePlayerState value, bool asServer)
        {
            if (asServer)
                return;
        }

        private void OnRaceTeamDataChanged(SyncDictionaryOperation op, int key, RaceTeamData value, bool asServer)
        {
            if (asServer)
                return;
        }

        private void OnLeaderboardChanged(SyncListOperation op, int index, int prev, int next, bool asServer)
        {
            if (asServer)
                return;
        }

        public void ShowFinishScreen() => _viewModel.SetShowFinishScreen(true);
        internal void ShowCountdown() => _viewModel.SetShowCountdown();
    }
}
