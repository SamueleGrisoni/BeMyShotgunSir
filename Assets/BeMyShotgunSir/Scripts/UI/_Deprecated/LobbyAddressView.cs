using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LobbyAddressView : MonoBehaviour
    {
        [SerializeField] private InterfaceSerializer<SOLobbyData, ILobbyDataView> _lobbyData;
        private ILobbyDataView _view;
        private TextMeshProUGUI _ipText;
        private void Awake()
        {
            TryGetComponent(out _ipText);
            _view = _lobbyData.Interface;
            Debug.Assert(_view != null, "LobbyAddressView requires a reference to an ILobbyDataView.");
        }

        private void OnEnable()
        {
            if (_view != null) _view.OnLobbyIPChanged += UpdateLobbyIP;
            UpdateLobbyIP();
        }

        private void OnDisable()
        {
            if (_view != null) _view.OnLobbyIPChanged -= UpdateLobbyIP;
        }

        private void UpdateLobbyIP()
        {
            if (_ipText != null && _view != null)
            {
                _ipText.text = $"{_view.LobbyIP}";
            }
        }
    }
}
