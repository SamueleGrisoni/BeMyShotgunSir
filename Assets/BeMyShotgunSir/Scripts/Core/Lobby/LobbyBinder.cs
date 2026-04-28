namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public class LobbyBinder : Binder<LobbyCommand, ILobbyDataView, ILobbyBindTarget>
    {
        public LobbyBinder(LobbyCommand lobbyCommand, ILobbyDataView lobbyDataView) : base(lobbyCommand, lobbyDataView) { }
    }
}
