namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyNetContext
    {
        public LobbyNetStateStore NetState { get; }
        public ILobbyNetController NetController { get; }
        public ILobbyManager Manager { get; }
        public LobbyClientProjector ClientProjector { get; }

        public LobbyNetContext(ILobbyManager manager, ILobbyNetController netController, LobbyNetStateStore netState, LobbyClientProjector clientProjector)
        {
            Manager = manager;
            NetController = netController;
            NetState = netState;
            ClientProjector = clientProjector;
        }
    }
}
