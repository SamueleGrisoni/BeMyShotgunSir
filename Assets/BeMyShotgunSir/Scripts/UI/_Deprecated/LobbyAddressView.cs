using BeMyShotgunSir.Scripts.Core.Lobby;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LobbyAddressView : LobbyBindTarget
    {
        private TextMeshProUGUI _ipText;
        private LobbyCommand _lobbyCommand;
        private ILobbyDataView _lobbyDataView;
        public override void BindLobbyCommand(LobbyCommand lobbyCommand) => _lobbyCommand = lobbyCommand;
        public override void BindLobbyDataView(ILobbyDataView lobbyDataView)
        {
            _lobbyDataView = lobbyDataView;
            _lobbyDataView.OnLobbyIPChanged += UpdateLobbyIP;
            UpdateLobbyIP();
        }

        private void Awake() =>
            TryGetComponent(out _ipText);

        private void OnDisable()
        {
            if (_lobbyDataView != null) _lobbyDataView.OnLobbyIPChanged -= UpdateLobbyIP;
        }

        private void UpdateLobbyIP()
        {
            if (_ipText == null) TryGetComponent(out _ipText);
            if (_lobbyDataView != null)
            {
                _ipText.text = $"{_lobbyDataView.LobbyIP}";
            }
        }

    }
}
