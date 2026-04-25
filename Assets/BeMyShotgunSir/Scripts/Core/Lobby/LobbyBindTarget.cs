namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyBindTarget : IBindTarget<LobbyCommand, ILobbyDataView> { }
    public abstract class LobbyBindTarget : BindTarget<LobbyCommand, ILobbyDataView>, ILobbyBindTarget { }
}
