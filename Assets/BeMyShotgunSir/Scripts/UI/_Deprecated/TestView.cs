using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TestView : LobbyBindTarget
    {
        private LobbyCommand _lobbyCommand;
        private ILobbyDataView _lobbyDataView;
        private TextMeshProUGUI _testText;

        private void Awake() =>
            TryGetComponent(out _testText);

        public override void BindLobbyCommand(LobbyCommand lobbyCommand) => _lobbyCommand = lobbyCommand;
        public override void BindLobbyDataView(ILobbyDataView lobbyData)
        {
            _lobbyDataView = lobbyData;
            _lobbyDataView.OnPlayerStatesChanged += UpdateTestView;
            UpdateTestView();
        }

        private void OnDisable()
        {
            if (_lobbyDataView != null) _lobbyDataView.OnPlayerStatesChanged -= UpdateTestView;
        }

        private void UpdateTestView()
        {
            if (_testText == null || _lobbyDataView == null)
                return;

            Dictionary<int, PlayerLobbyState> states = _lobbyDataView.PlayerStates;
            if (states == null || states.Count == 0)
            {
                _testText.text = "No players";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<int, PlayerLobbyState> kvp in states)
            {
                PlayerLobbyState s = kvp.Value;
                sb.AppendLine($"[{kvp.Key}] Id={s.ConnectionId} Name={s.PlayerName} Ready={s.IsReady}");
            }

            _testText.text = sb.ToString();
        }


    }
}
