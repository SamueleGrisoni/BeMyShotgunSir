namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyCommand
    {
        private readonly LobbyManager _lobbyManager;
        private readonly LobbyNetController _netController;
        public LobbyCommand(LobbyManager lobbyManager, LobbyNetController netController)
        {
            _lobbyManager = lobbyManager;
            _netController = netController;
        }

        public void QuitLobby_CMRequest() =>
            GameServices.Instance.ConnectionManager.QuitLobby();

        public void GetInitSnapshot_Request() =>
            _netController.HandleRefresh();

        public void SetName_Request(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            _netController.UpdatePlayerName_ServerRpc(name);
        }

        public void SetPlayerReady_Request(bool isReady) =>
            _netController.UpdatePlayerReady_ServerRpc(isReady);

    }
}
