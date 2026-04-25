using BeMyShotgunSir.Scripts.Core.Lobby;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class PlayerCountView : LobbyBindTarget
    {
        private TextMeshProUGUI _playerCountText;

        private void Awake() =>
            TryGetComponent(out _playerCountText);

        public override void OnBindComplete()
        {
            _dataView.OnPlayerCountChanged += UpdatePlayerCount;
            UpdatePlayerCount();
        }

        private void OnDisable()
        {
            if (_dataView != null) _dataView.OnPlayerCountChanged -= UpdatePlayerCount;
        }

        private void UpdatePlayerCount()
        {
            if (_playerCountText != null && _dataView != null)
            {
                _playerCountText.text = $"{_dataView.PlayerCount}";
            }
        }
    }
}
