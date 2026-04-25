using BeMyShotgunSir.Scripts.Core.Lobby;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class PlayerCountView : LobbyBindTarget
    {
        private LobbyCommand _lobbyCommand;
        private ILobbyDataView _lobbyDataView;
        private TextMeshProUGUI _playerCountText;

        private void Awake() =>
            TryGetComponent(out _playerCountText);

        public override void BindLobbyCommand(LobbyCommand lobbyCommand) => _lobbyCommand = lobbyCommand;
        public override void BindLobbyDataView(ILobbyDataView lobbyData) => _lobbyDataView = lobbyData;

        public override void OnBindComplete()
        {
            _lobbyDataView.OnPlayerCountChanged += UpdatePlayerCount;
            UpdatePlayerCount();
        }

        private void OnDisable()
        {
            if (_lobbyDataView != null) _lobbyDataView.OnPlayerCountChanged -= UpdatePlayerCount;
        }

        private void UpdatePlayerCount()
        {
            if (_playerCountText != null && _lobbyDataView != null)
            {
                _playerCountText.text = $"{_lobbyDataView.PlayerCount}";
            }
        }
    }
}
