using FishNet.Object.Synchronizing;
using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Gameplay.Track;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceProjector
    {
        void Init(RaceViewModel viewModel);
        void InitNetData_Project(IRaceNetData data);
    }

    /// <summary>
    /// Client-side projection layer for race local effects:
    /// - Sync collections callbacks
    /// - RPC local actions applied to manager/viewmodel side
    /// </summary>
    [RequireComponent(typeof(RaceManager))]
    [RequireComponent(typeof(RaceNetController))]
    [RequireComponent(typeof(RaceNetStateStore))]
    public sealed class RaceClientProjector : MonoBehaviour, IRaceProjector
    {
        //utility
        private bool _log = true;

        //references
        private IRaceNetStateSubscribe _state;
        private RaceViewModel _viewModel;
        private IRoadManager _roadManager;
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

        private void OnEnable() =>
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;

        private void OnRoadManagerSpawned(IRoadManager manager) => _roadManager = manager;

        public void Init(RaceViewModel viewModel)
        {
            if (_viewModel == null)
                _viewModel = viewModel;

            if (_state == null)
                return;

            _state.PlayerStatesSync.OnChange += OnRacePlayerStateChanged;
            _state.TeamDataSync.OnChange += OnRaceTeamDataChanged;
            _state.LeaderboardSync.OnChange += OnLeaderboardChanged;
        }

        private void OnDisable()
        {
            if (_state == null)
                return;

            _state.PlayerStatesSync.OnChange -= OnRacePlayerStateChanged;
            _state.TeamDataSync.OnChange -= OnRaceTeamDataChanged;
            _state.LeaderboardSync.OnChange -= OnLeaderboardChanged;

            RoadManager.OnRoadManagerSpawned -= OnRoadManagerSpawned;
        }

        private void OnRacePlayerStateChanged(SyncDictionaryOperation op, int key, RacePlayerState value, bool asServer)
        {
            if (asServer)
                return;
            _viewModel.SetPlayerStates(op, key, value);
        }

        private void OnRaceTeamDataChanged(SyncDictionaryOperation op, int key, RaceTeamData value, bool asServer)
        {
            if (asServer)
                return;
            _viewModel.SetTeamData(op, key, value);
        }

        private void OnLeaderboardChanged(SyncListOperation op, int index, int prev, int next, bool asServer)
        {
            if (asServer)
                return;
            _viewModel.SetLeaderboard(op, index, prev, next);
        }

        public void InitNetData_Project(IRaceNetData data) => _viewModel.InitNetData(data);
    }
}
