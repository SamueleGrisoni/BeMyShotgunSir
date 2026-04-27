using System.Collections;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyViewController : LobbyBindTarget
    {
        [SerializeField] private UIController_Lobby _uiControllerLobby;
        [SerializeField] private UIDocument _lobbyDocument;

        public override void OnBindComplete()
        {
            if (_command == null)
            {
                Debug.LogError("LobbyCommand not bound to LobbyViewController!");
                return;
            }
            if (_dataView == null)
            {
                Debug.LogError("LobbyDataView not bound to LobbyViewController!");
                return;
            }

            _dataView.OnLobbyIPChanged += UpdateLobbyIP;
            _dataView.OnPlayerCountChanged += UpdatePlayerCount;
            // NOTE the following is commented since not really needed (as LobbyInfo should not change). The choice is up to @OmegaMorello
            // _dataView.OnLobbyInfoChanged += UpdateLobbyInfo;

            //NOTE the following two instructions are redundant since as soon as the bind occurs, the SOData will trigger the events
            UpdatePlayerCount();
            UpdateLobbyIP();
        }


        #region UI Elements
        private VisualElement _root;
        private Label _lobbyIPAddress;
        private Label _playersInLobby;
        private Button _startRaceButton;
        private Button _leaveLobbyButton;
        private Button _backButton;
        #endregion

        private void OnEnable()
        {
            if (_lobbyDocument == null) return;
            _root = _lobbyDocument.rootVisualElement;
            StartCoroutine(InitNextFrame());
            Show(false);
        }

        private IEnumerator InitNextFrame()
        {
            _lobbyIPAddress = _root.Q<Label>("LobbyIPAddress");
            _playersInLobby = _root.Q<Label>("PlayersInLobby");
            _startRaceButton = _root.Q<Button>("StartRaceButton");
            _leaveLobbyButton = _root.Q<Button>("LeaveLobbyButton");

            _backButton = _root.Q<Button>("BackButton");

            yield return null;

            _startRaceButton.clicked += StartRaceButtonHandler;
            _leaveLobbyButton.clicked += LeaveLobbyButtonHandler;

            _backButton.clicked += BackButtonHandler;

            _lobbyIPAddress.text = "LOBBY ADDRESS: localhost:7770";
            _playersInLobby.text = "PLAYERS IN LOBBY: 1/4";
        }



        private void StartRaceButtonHandler()
        {
            // TODO: Implement start
            Debug.Log("Start Race button clicked");
            // GameServices.Instance.ConnectionManager.StartGame();
        }

        private void LeaveLobbyButtonHandler()
        {
            // TODO: Save on a SO which screen to show after leaving the lobby
            GameServices.Instance.UIFlowState.SetNextState(UIScreen.HostOrJoin);
            CloseConnection();
        }

        private void BackButtonHandler()
        {
            CloseConnection();
            _uiControllerLobby.ShowScreen(UIScreen.CentralHub, true);
        }


        private void CloseConnection() => _command.QuitLobby_CMRequest();

        // This method can be used to change the player name
        private void SetName()
        {
            _command.SetName_Request("TEST NAME");
        }

        private void UpdateLobbyIP()
        {
            if (_lobbyIPAddress != null && _dataView != null)
            {
                _lobbyIPAddress.text = $"{_dataView.LobbyInfo.LobbyIP}";
            }
        }

        private void UpdatePlayerCount()
        {
            if (_playersInLobby != null && _dataView != null)
            {
                _playersInLobby.text = $"PLAYERS IN LOBBY: {_dataView.PlayerCount}/{_dataView.LobbyInfo.MaxPlayers}";
            }
        }

        private void OnDisable()
        {
            if (_dataView != null) _dataView.OnLobbyIPChanged -= UpdateLobbyIP;

            _startRaceButton.clicked -= StartRaceButtonHandler;
            _leaveLobbyButton.clicked -= LeaveLobbyButtonHandler;

            _backButton.clicked -= BackButtonHandler;
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
