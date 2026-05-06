using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    /// <summary>
    /// Client-side projection layer: maps synchronized network state and local RPC payloads
    /// into manager/viewmodel response calls.
    /// </summary>
    [RequireComponent(typeof(LobbyNetStateStore))]
    public sealed class LobbyClientProjector : MonoBehaviour
    {
        private ILobbyNetStateSubscribe _state;
        private LobbyViewModel _viewModel;
        [SerializeField] private SOLobbySounds _sounds;
        private SOAudioRequestEvent _audioRequestEvent;

        private void Awake()
        {
            _state = GetComponent<LobbyNetStateStore>();
            if (_state == null)
                Debug.LogError("LobbyClientProjection requires LobbyNetStateStore on the same GameObject.", this);
            _audioRequestEvent = GameServices.Instance.Channels.AudioRequestEvent;

        }

        private void OnEnable()
        {
            if (_state == null)
                return;

            _state.LobbyInfoSync.OnChange += OnLobbyInfoChanged;
            _state.PlayerStatesSync.OnChange += OnPlayerStatesChanged;
            _state.TeamInfosSync.OnChange += OnTeamInfosChanged;
            _state.PlayerCountSync.OnChange += OnPlayerCountChanged;
        }

        private void OnDisable()
        {
            if (_state == null)
                return;

            _state.LobbyInfoSync.OnChange -= OnLobbyInfoChanged;
            _state.PlayerStatesSync.OnChange -= OnPlayerStatesChanged;
            _state.TeamInfosSync.OnChange -= OnTeamInfosChanged;
            _state.PlayerCountSync.OnChange -= OnPlayerCountChanged;
        }

        public void Init(LobbyViewModel viewModel)
        {
            if (_viewModel == null)
                _viewModel = viewModel;
        }

        public void InitNetData_Response(LobbyNetDataSnapshot snapshot) => _viewModel.InitNetData(snapshot);

        private void OnLobbyInfoChanged(LobbyInfo prev, LobbyInfo next, bool asServer)
        {
            if (asServer)
                return;

            if (_viewModel != null)
                _viewModel.SetLobbyInfo(next);
        }

        private void OnPlayerStatesChanged(SyncDictionaryOperation op, int key, LobbyPlayerState value, bool asServer)
        {
            if (asServer)
                return;

            if (_viewModel != null)
                _viewModel.SetPlayerStates(op, key, value);
        }

        private void OnTeamInfosChanged(SyncDictionaryOperation op, int key, LobbyTeamInfo value, bool asServer)
        {
            if (asServer)
                return;

            if (_viewModel != null)
                _viewModel.SetTeamInfos(op, key, value);
        }

        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (asServer)
                return;

            if (_viewModel != null)
                _viewModel.SetPlayerCount(next);
        }
    }
}
