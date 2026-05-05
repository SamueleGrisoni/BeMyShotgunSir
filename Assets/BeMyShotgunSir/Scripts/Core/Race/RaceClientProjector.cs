using FishNet.Object.Synchronizing;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using UnityEngine;
using BeMyShotgunSir.Scripts.Utils;
using BeMyShotgunSir.Scripts.Gameplay.Track;
using FishNet.Object;
using Unity.Cinemachine;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceProjector
    {
        void Init(RaceViewModel viewModel);
        void InitNetData_Project(IRaceNetData data);
        void SetUpPlayer_Response(NetworkObject player, RaceRole role);
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
        private IRaceNetStateStore_Projector _state;
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

        private void OnEnable()
        {
            RoadManager.OnRoadManagerSpawned += OnRoadManagerSpawned;
        }

        private void OnRoadManagerSpawned(IRoadManager manager)
        {
            if (_roadManager == null)
                _roadManager = manager as RoadManager;
        }

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

        public void SetRoadManager(RoadManager roadManager)
        {
            if (_roadManager == null)
                _roadManager = roadManager;
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

        public void SetUpPlayer_Response(NetworkObject player, RaceRole role)
        {
            //TODO setup viewmodel with role
            MovingDriver driver = player.GetComponentInChildren<MovingDriver>();
            if (driver == null)
            {
                Log.ELazy(() => $"Player prefab {player.name} is missing a driver component. Cannot initialize race for this player.", this);
                return;
            }
            CinemachineCamera cam = player.GetComponentInChildren<CinemachineCamera>();
            if (cam == null)
            {
                Log.ELazy(() => $"Player prefab {player.name} is missing a CinemachineCamera component. Cannot initialize race camera for this player.", this);
                return;
            }
            cam.enabled = true;
            _roadManager.SetDriver(driver.gameObject.transform);

            //TODO FINAL BIND

        }


    }
}
