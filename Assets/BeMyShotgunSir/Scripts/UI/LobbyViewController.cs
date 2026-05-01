using System.Collections;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Core;
using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace BeMyShotgunSir.Scripts.UI
{
    public class LobbyViewController : LobbyBindTarget
    {
        [SerializeField] private UIController_Lobby _uiControllerLobby;
        [SerializeField] private UIDocument _lobbyDocument;

        #region Bindings
        // INITIAL BINDING
        private LobbyCommand _command;
        private LobbyViewModel _viewModel;
        /// FINAL BINDING
        #endregion

        public override void OnInitialBindComplete()
        {
            if (_initialBindSource == null)
            {
                Log.ELazy(() => "Initial bind source is null. Cannot complete initial bind.", this);
                return;
            }
            _command = _initialBindSource.Command;
            _viewModel = _initialBindSource.ViewModel;

            _viewModel.OnLobbyIPChanged += UpdateLobbyIP;
            _viewModel.OnPlayerCountChanged += UpdatePlayerCount;
            _viewModel.OnPlayerStatesChanged += UpdatePlayerStates;
        }
        public override void OnFinalBindComplete()
        {
            if (_finalBindSource == null)
            {
                Log.ELazy(() => "Final bind source is null. Cannot complete final bind.", this);
                return;
            }
        }


        #region UI Elements
        private VisualElement _root;
        private Label _lobbyIPAddress;
        private Label _playersInLobby;
        private VisualElement _playersInfo;
        private readonly Dictionary<int, VisualElement> _playerRows = new();
        private Button _readyButton;
        private Button _leaveLobbyButton;
        private Button _backButton;
        private VisualElement _selectRoleContainer;
        private Button _driverButton;
        private Button _shotgunButton;
        #endregion

        private LobbyPlayerState[] _playerInfo;

        #region Public Properties
        public bool ShowSelectRole;
        #endregion

        private void OnEnable()
        {
            if (_lobbyDocument == null) return;
            _root = _lobbyDocument.rootVisualElement;
            _lobbyIPAddress = _root.Q<Label>("LobbyIPAddress");
            _playersInLobby = _root.Q<Label>("PlayersInLobby");
            _playersInfo = _root.Q<VisualElement>("PlayersInfo");

            _readyButton = _root.Q<Button>("ReadyButton");
            _leaveLobbyButton = _root.Q<Button>("LeaveLobbyButton");
            _backButton = _root.Q<Button>("BackButton");
            _selectRoleContainer = _root.Q<VisualElement>("SelectRoleContainer");
            _driverButton = _selectRoleContainer.Q<Button>("DriverButton");
            _shotgunButton = _selectRoleContainer.Q<Button>("ShotgunButton");

            StartCoroutine(InitNextFrame());
            Show(false);
        }

        private IEnumerator InitNextFrame()
        {
            yield return null;

            _readyButton.clicked += ReadyButtonHandler;
            _leaveLobbyButton.clicked += LeaveLobbyButtonHandler;
            _backButton.clicked += BackButtonHandler;
            _driverButton.clicked += DriverButtonHandler;
            _shotgunButton.clicked += ShotgunButtonHandler;
        }

        private void Update()
        {
            if (ShowSelectRole) _selectRoleContainer.style.display = DisplayStyle.Flex;
            else _selectRoleContainer.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            if (_viewModel != null)
            {
                _viewModel.OnLobbyIPChanged -= UpdateLobbyIP;
                _viewModel.OnPlayerCountChanged -= UpdatePlayerCount;
                _viewModel.OnPlayerStatesChanged -= UpdatePlayerStates;
            }

            _readyButton.clicked -= ReadyButtonHandler;
            _leaveLobbyButton.clicked -= LeaveLobbyButtonHandler;
            _backButton.clicked -= BackButtonHandler;
            _driverButton.clicked -= DriverButtonHandler;
            _shotgunButton.clicked -= ShotgunButtonHandler;

            _playerRows.Clear();
        }

        #region Handlers

        private void ReadyButtonHandler()
        {
            Debug.Log("Ready button clicked");
            _command.SetPlayerReady_Request(true);
            // _command.SelectTeamMate_Request(1);
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

        private void DriverButtonHandler()
        {
            Debug.Log("Driver button clicked");
        }

        private void ShotgunButtonHandler()
        {
            Debug.Log("Shotgun button clicked");
        }

        private void TeamRequestHandler(int teammateConnectionId)
        {
            Debug.Log($"Player row clicked, teammateConnectionId={teammateConnectionId}");
            _command.SelectTeamMate_Request(teammateConnectionId);
        }

        #endregion

        private void CloseConnection()
        {
            _command.LeaveTeam_Request();
            _command.QuitLobby_CMRequest();
        }


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

        private void ClearPlayerList()
        {
            if (_playersInfo == null) return;

            var rows = _playersInfo.Query<VisualElement>(className: "player-row").ToList();

            foreach (VisualElement row in rows)
            {
                if (row.ClassListContains("header"))
                    continue;

                row.RemoveFromHierarchy();
            }
        }

        private void UpdatePlayerStates()
        {
            ClearPlayerList();
            _playerRows.Clear();
            SyncPlayerRows();
        }


        private void SyncPlayerRows()
        {
            if (_playersInfo == null || _viewModel == null)
                return;

            int count = _viewModel.PlayerStates.Count;

            for (int i = 0; i < count; i++)
            {
                LobbyPlayerState playerState = _viewModel.PlayerStates[i];

                if (_playerRows.TryGetValue(i, out VisualElement row))
                {
                    UpdatePlayerRow(row, playerState);
                }
                else
                {
                    VisualElement newRow = BuildPlayerRow(playerState);
                    _playersInfo.Add(newRow);
                    _playerRows[i] = newRow;
                }
            }

            RemoveExtraRows(count);
        }

        private VisualElement BuildPlayerRow(LobbyPlayerState playerState)
        {
            var row = new VisualElement();
            row.AddToClassList("player-row");

            var name = new Label();
            name.name = "Name";
            name.AddToClassList("player-name");

            var team = new Label();
            team.name = "Team";
            team.AddToClassList("player-team");

            var ready = new VisualElement();
            ready.name = "Ready";
            ready.AddToClassList("ready");

            var readyLabel = new Label("READY");
            readyLabel.name = "ReadyLabel";
            readyLabel.AddToClassList("ready-label");
            ready.Add(readyLabel);

            row.Add(name);
            row.Add(team);
            row.Add(ready);

            name.RegisterCallback<ClickEvent>(evt => TeamRequestHandler(playerState.ConnectionId));

            UpdatePlayerRow(row, playerState);

            return row;
        }

        private void UpdatePlayerRow(VisualElement row, LobbyPlayerState playerState)
        {
            Label name = row.Q<Label>("Name");
            Label team = row.Q<Label>("Team");
            VisualElement ready = row.Q<VisualElement>("Ready");

            if (name != null)
                name.text = playerState.PlayerName;

            if (team != null)
                team.text = (playerState.TeamId != -1) ? playerState.TeamId.ToString() : "-";

            if (ready != null)
                ready.style.opacity = playerState.IsReady ? 1f : 0f;

            if (playerState.TeamId != -1) name.AddToClassList("player-in-team");
            else name.RemoveFromClassList("player-in-team");
        }

        private void RemoveExtraRows(int validCount)
        {
            List<int> toRemove = new();

            foreach (int index in _playerRows.Keys)
            {
                if (index >= validCount)
                    toRemove.Add(index);
            }

            foreach (int index in toRemove)
            {
                _playerRows[index].RemoveFromHierarchy();
                _playerRows.Remove(index);
            }
        }

        public void Show(bool show) => _root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

    }
}
