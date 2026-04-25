using System.Collections;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyViewController : LobbyBindTarget
    {
        [SerializeField] private UIManager _uIManager;
        [SerializeField] private UIDocument _lobbyMenuDocument;

        #region Lobby Data & Commands
        private LobbyCommand _command;
        private ILobbyDataView _view;

        public override void BindLobbyCommand(LobbyCommand lobbyCommand) => _command = lobbyCommand;
        public override void BindLobbyDataView(ILobbyDataView lobbyData) => _view = lobbyData;
        public override void OnBindComplete()
        {
            if (_command == null)
            {
                Debug.LogError("LobbyCommand not bound to LobbyViewController!");
                return;
            }
            if (_view == null)
            {
                Debug.LogError("LobbyDataView not bound to LobbyViewController!");
                return;
            }

            _view.OnLobbyIPChanged += UpdateLobbyIP;
            _view.OnPlayerCountChanged += UpdatePlayerCount;
            // NOTE the following is commented since not really needed (as LobbyInfo should not change). The choice is up to @OmegaMorello
            // _view.OnLobbyInfoChanged += UpdateLobbyInfo;

            //NOTE the following two instructions are redundant since as soon as the bind occurs, the SOData will trigger the events
            UpdatePlayerCount();
            UpdateLobbyIP();
        }
        #endregion


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

        private void OnEnable()
        {
            if (_lobbyMenuDocument == null) return;
            _root = _lobbyMenuDocument.rootVisualElement;
            StartCoroutine(InitNextFrame());
            Show(false);
        }

        private IEnumerator InitNextFrame()
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
            _command.QuitLobby_CMRequest();
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
                _playersInLobby.text = $"PLAYERS IN LOBBY: {_view.PlayerCount}/{_view.LobbyInfo.MaxPlayers}";
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
