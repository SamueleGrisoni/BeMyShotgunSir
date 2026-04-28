using BeMyShotgunSir.Scripts.Core.Lobby;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LobbyAddressView : LobbyBindTarget
    {
        private TextMeshProUGUI _ipText;

        public override void OnBindComplete()
        {
            _viewModel.OnLobbyIPChanged += UpdateLobbyIP;
            UpdateLobbyIP();
        }

        private void Awake() =>
            TryGetComponent(out _ipText);

        private void OnDisable()
        {
            if (_viewModel != null) _viewModel.OnLobbyIPChanged -= UpdateLobbyIP;
        }

        private void UpdateLobbyIP()
        {
            if (_ipText == null) TryGetComponent(out _ipText);
            if (_viewModel != null)
            {
                _ipText.text = $"{_viewModel.LobbyInfo.LobbyIP}";
            }
        }

    }
}
