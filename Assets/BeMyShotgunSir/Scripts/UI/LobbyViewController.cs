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
            if (_viewModel == null)
            {
                Debug.LogError("LobbyDataView not bound to LobbyViewController!");
                return;
            }

            _viewModel.OnLobbyIPChanged += UpdateLobbyIP;
            _viewModel.OnPlayerCountChanged += UpdatePlayerCount;
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
        }



        private void StartRaceButtonHandler()
        {
            // TODO: Implement start
            Debug.Log("Start Race button clicked");
            // GameServices.Instance.ConnectionManager.StartGame();
        }

        private void LeaveLobbyButtonHandler()
        {
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
            if (_lobbyIPAddress != null && _viewModel != null)
            {
                _lobbyIPAddress.text = $"{_viewModel.LobbyInfo.LobbyIP}";
            }
        }

        private void UpdatePlayerCount()
        {
            if (_playersInLobby != null && _viewModel != null)
            {
                _playersInLobby.text = $"PLAYERS IN LOBBY: {_viewModel.PlayerCount}/{_viewModel.LobbyInfo.MaxPlayers}";
            }
        }

        private void OnDisable()
        {
            if (_viewModel != null) _viewModel.OnLobbyIPChanged -= UpdateLobbyIP;
            if (_viewModel != null) _viewModel.OnPlayerCountChanged -= UpdatePlayerCount;

            _startRaceButton.clicked -= StartRaceButtonHandler;
            _leaveLobbyButton.clicked -= LeaveLobbyButtonHandler;

            _backButton.clicked -= BackButtonHandler;
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
