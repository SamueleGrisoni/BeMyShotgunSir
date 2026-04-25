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
            _dataView.OnLobbyIPChanged += UpdateLobbyIP;
            UpdateLobbyIP();
        }

        private void Awake() =>
            TryGetComponent(out _ipText);

        private void OnDisable()
        {
            if (_dataView != null) _dataView.OnLobbyIPChanged -= UpdateLobbyIP;
        }

        private void UpdateLobbyIP()
        {
            if (_ipText == null) TryGetComponent(out _ipText);
            if (_dataView != null)
            {
                _ipText.text = $"{_dataView.LobbyIP}";
            }
        }

    }
}
