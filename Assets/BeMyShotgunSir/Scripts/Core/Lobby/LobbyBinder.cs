namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyBinder
    {
        private readonly LobbyCommand _lobbyCommand;
        private readonly ILobbyDataView _lobbyDataView;
        public LobbyBinder(LobbyCommand lobbyCommand, ILobbyDataView lobbyDataView)
        {
            _lobbyCommand = lobbyCommand;
            _lobbyDataView = lobbyDataView;
        }
        public void BindCommand(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.BindLobbyCommand(_lobbyCommand);
            }
        }

        public void BindData(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.BindLobbyDataView(_lobbyDataView);
            }
        }
    }
}
