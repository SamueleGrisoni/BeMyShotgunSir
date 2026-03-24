using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class PlayerCountView : MonoBehaviour
    {
        [SerializeField] private InterfaceSerializer<SOLobbyData, ILobbyDataView> _lobbyData;
        private ILobbyDataView _view;
        private TextMeshProUGUI _playerCountText;
        private void Awake()
        {
            TryGetComponent(out _playerCountText);
            _view = _lobbyData.Interface;
            Debug.Assert(_view != null, "PlayerCountView requires a reference to an ILobbyDataView.");
        }

        private void OnEnable()
        {
            if (_view != null) _view.OnPlayerCountChanged += UpdatePlayerCount;
            UpdatePlayerCount();
        }

        private void OnDisable()
        {
            if (_view != null) _view.OnPlayerCountChanged -= UpdatePlayerCount;
        }

        private void UpdatePlayerCount()
        {
            if (_playerCountText != null && _view != null)
            {
                _playerCountText.text = $"{_view.PlayerCount}";
            }
        }
    }
}
