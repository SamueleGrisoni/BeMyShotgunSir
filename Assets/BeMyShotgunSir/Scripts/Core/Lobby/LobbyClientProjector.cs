using BeMyShotgunSir.Scripts.Events;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Core.Lobby
{
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

            _state.LobbyInfo_Sub.OnChange += OnLobbyInfoChanged;
            _state.PlayerStates_Sub.OnChange += OnPlayerStatesChanged;
            _state.TeamInfos_Sub.OnChange += OnTeamInfosChanged;
            _state.PlayerCount_Sub.OnChange += OnPlayerCountChanged;
        }

        private void OnDisable()
        {
            if (_state == null)
                return;

            _state.LobbyInfo_Sub.OnChange -= OnLobbyInfoChanged;
            _state.PlayerStates_Sub.OnChange -= OnPlayerStatesChanged;
            _state.TeamInfos_Sub.OnChange -= OnTeamInfosChanged;
            _state.PlayerCount_Sub.OnChange -= OnPlayerCountChanged;
        }

        public void Init(LobbyViewModel viewModel)
        {
            if (_viewModel == null)
                _viewModel = viewModel;
        }

        private void OnLobbyInfoChanged(LobbyInfo prev, LobbyInfo next, bool asServer)
        {
            if (asServer)
                return;
        }

        private void OnPlayerStatesChanged(SyncDictionaryOperation op, int key, LobbyPlayerState value, bool asServer)
        {
            if (asServer)
                return;
        }

        private void OnTeamInfosChanged(SyncDictionaryOperation op, int key, LobbyTeamInfo value, bool asServer)
        {
            if (asServer)
                return;
        }

        private void OnPlayerCountChanged(int prev, int next, bool asServer)
        {
            if (asServer)
                return;
        }
    }
}
