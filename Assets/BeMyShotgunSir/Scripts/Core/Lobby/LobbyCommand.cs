namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyCommand : ICommand
    {
        private ILobbyNetController_Command _netController;
        public LobbyCommand(ILobbyNetController_Command netController)
        {
            _netController = netController;
        }

        public void QuitLobby_CMRequest() =>
            GameServices.Instance.ConnectionManager.QuitLobby();

        public void SetName_Request(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            _netController.UpdatePlayerName_ServerRpc(name);
        }

        public void SetPlayerReady_Request(bool isReady) =>
            _netController.UpdatePlayerReady_ServerRpc(isReady);

        public void SelectTeamMate_Request(int teammateConnectionId) =>
            _netController.SelectTeamMate_ServerRpc(teammateConnectionId);

        public void LeaveTeam_Request() =>
            _netController.LeaveTeam_ServerRpc();

    }
}
