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
    }
}
