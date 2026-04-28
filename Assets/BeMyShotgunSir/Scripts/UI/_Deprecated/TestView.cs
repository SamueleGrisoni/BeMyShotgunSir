using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core.Lobby;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TestView : LobbyBindTarget
    {
        private TextMeshProUGUI _testText;

        public override void OnBindComplete()
        {
            _viewModel.OnPlayerStatesChanged += UpdateTestView;
            UpdateTestView();
        }

        private void Awake() =>
            TryGetComponent(out _testText);

        private void OnDisable()
        {
            if (_viewModel != null) _viewModel.OnPlayerStatesChanged -= UpdateTestView;
        }

        private void UpdateTestView()
        {
            if (_testText == null || _viewModel == null)
                return;

            Dictionary<int, PlayerLobbyState> states = _viewModel.PlayerStates;
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
