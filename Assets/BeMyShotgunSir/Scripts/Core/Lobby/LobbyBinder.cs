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

        public void Bind(ILobbyBindTarget[] targets)
        {
            BindCommand(targets);
            BindData(targets);
            CompleteBinding(targets);
        }
        private void BindCommand(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.BindCommand(_lobbyCommand);
            }
        }

        private void BindData(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.BindDataView(_lobbyDataView);
            }
        }

        private void CompleteBinding(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.OnBindComplete();
            }
        }
    }
}
