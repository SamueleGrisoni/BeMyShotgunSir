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
            CompleteBinding();
        }
        private void BindCommand(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.BindLobbyCommand(_lobbyCommand);
            }
        }

        private void BindData(ILobbyBindTarget[] bindTargets)
        {
            foreach (ILobbyBindTarget target in bindTargets)
            {
                target.BindLobbyDataView(_lobbyDataView);
            }
        }

        private void CompleteBinding()
        {
            foreach (ILobbyBindTarget target in GameServices.Instance.LobbyManager.GetComponentsInChildren<ILobbyBindTarget>())
            {
                target.OnBindComplete();
            }
        }
    }
}
