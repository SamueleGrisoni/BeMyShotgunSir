using System.Collections;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyViewController : MonoBehaviour
    {
        [SerializeField] private UIManager _uIManager;
        [SerializeField] private UIDocument _lobbyMenuDocument;
        [SerializeField] private InterfaceSerializer<SOLobbyData, ILobbyDataView> _lobbyData;

        #region UI Elements
        private VisualElement _root;
        private VisualElement _hostJoinButtonContainer;
        private Button _hostButton;
        private Button _joinButton;
        private TextField _joinIPAddress;
        private VisualElement _lobbyInfoContainer;
        private Label _lobbyIPAddress;
        private Label _playersInLobby;
        private Button _startRaceButton;
        private Button _leaveLobbyButton;
        private Button _backButton;
        #endregion

        #region Lobby Data
        private ILobbyDataView _view;
        private int _maxPlayers = 4;
        #endregion

        private void Awake()
        {
            _view = _lobbyData.Interface;
            Debug.Assert(_view != null, "LobbyViewController requires a reference to an ILobbyDataView.");
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.OnLobbyIPChanged += UpdateLobbyIP;
                _view.OnPlayerCountChanged += UpdatePlayerCount;
            }
            UpdatePlayerCount();
            UpdateLobbyIP();

            if (_lobbyMenuDocument == null) return;
            _root = _lobbyMenuDocument.rootVisualElement;
            StartCoroutine(InitNextFrame());
            Show(false);
        }

        IEnumerator InitNextFrame()
        {
            _hostJoinButtonContainer = _root.Q<VisualElement>("HostJoinButtonContainer");
            _hostButton = _root.Q<Button>("HostButton");
            _joinButton = _root.Q<Button>("JoinButton");
            _joinIPAddress = _root.Q<TextField>("JoinIPAddress");

            _lobbyInfoContainer = _root.Q<VisualElement>("LobbyInfoContainer");
            _lobbyIPAddress = _root.Q<Label>("LobbyIPAddress");
            _playersInLobby = _root.Q<Label>("PlayersInLobby");
            _startRaceButton = _root.Q<Button>("StartRaceButton");
            _leaveLobbyButton = _root.Q<Button>("LeaveLobbyButton");

            _backButton = _root.Q<Button>("BackButton");

            yield return null;

            _hostButton.clicked += HostButtonHandler;
            _joinButton.clicked += JoinButtonHandler;
            _joinIPAddress.RegisterValueChangedCallback(IpAddressChanged);

            _startRaceButton.clicked += StartRaceButtonHandler;
            _leaveLobbyButton.clicked += LeaveLobbyButtonHandler;

            _backButton.clicked += BackButtonHandler;

            _joinIPAddress.value = "localhost";
            _lobbyIPAddress.text = "LOBBY ADDRESS: localhost:7770";
            _playersInLobby.text = "PLAYERS IN LOBBY: 1/4";
            ShowHostJoinButtons();
        }

        private void HostButtonHandler()
        {
            Debug.Log("Host button clicked");

            GameServices.Instance.ConnectionManager.StartHost();
            ShowLobbyContainer(host: true);
        }

        private void JoinButtonHandler()
        {
            Debug.Log("Join button clicked with IP: " + _joinIPAddress.value);

            GameServices.Instance.ConnectionManager.StartJoin(_joinIPAddress.value);
            ShowLobbyContainer();
        }

        private void IpAddressChanged(ChangeEvent<string> evt)
        {
            Debug.Log("IP Address changed to: " + evt.newValue);

            _joinIPAddress.value = evt.newValue;
        }

        private void StartRaceButtonHandler()
        {
            // TODO: Implement start
            Debug.Log("Start Race button clicked");
            // GameServices.Instance.ConnectionManager.StartGame();
        }

        private void LeaveLobbyButtonHandler()
        {
            Debug.Log("Leave Lobby button clicked");
            CloseConnection();
        }

        private void BackButtonHandler()
        {
            Debug.Log("Back button clicked");
            CloseConnection();
            _uIManager.ShowScreen(UIScreen.CentralHub, true);
        }

        private void ShowHostJoinButtons()
        {
            _hostJoinButtonContainer.style.display = DisplayStyle.Flex;
            _lobbyInfoContainer.style.display = DisplayStyle.None;
        }
        private void ShowLobbyContainer(bool host = false)
        {
            _lobbyInfoContainer.style.display = DisplayStyle.Flex;
            _hostJoinButtonContainer.style.display = DisplayStyle.None;

            if (host)
                _startRaceButton.style.display = DisplayStyle.Flex;
            else
                _startRaceButton.style.display = DisplayStyle.None;
        }

        private void CloseConnection()
        {
            GameServices.Instance.ConnectionManager.ChangeState(InitManagerState.Init);
            ShowHostJoinButtons();
        }

        private void UpdateLobbyIP()
        {
            if (_lobbyIPAddress != null && _view != null)
            {
                _lobbyIPAddress.text = $"{_view.LobbyIP}";
            }
        }

        private void UpdatePlayerCount()
        {
            if (_playersInLobby != null && _view != null)
            {
                _playersInLobby.text = $"PLAYERS IN LOBBY: {_view.PlayerCount}/{_maxPlayers}";
            }
        }

        private void OnDisable()
        {
            if (_view != null) _view.OnLobbyIPChanged -= UpdateLobbyIP;

            _hostButton.clicked -= HostButtonHandler;
            _joinButton.clicked -= JoinButtonHandler;
            _joinIPAddress.UnregisterValueChangedCallback(IpAddressChanged);

            _startRaceButton.clicked -= StartRaceButtonHandler;
            _leaveLobbyButton.clicked -= LeaveLobbyButtonHandler;

            _backButton.clicked -= BackButtonHandler;
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;


    }
}
