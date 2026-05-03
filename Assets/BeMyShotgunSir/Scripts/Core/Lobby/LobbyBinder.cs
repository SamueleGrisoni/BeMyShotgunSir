namespace BeMyShotgunSir.Scripts.Core.Lobby
{
    public interface ILobbyInitialBindSource : IBindSource
    {
        LobbyCommand Command { get; }
        LobbyViewModel ViewModel { get; }
    }
    public interface ILobbyFinalBindSource : IBindSource { }
    public interface ILobbyBindSource : IBindSource { }
    public interface ILobbyBindSources : ILobbyInitialBindSource, ILobbyFinalBindSource, IBindSources { }
    public interface ILobbyBindTarget : IBindTarget<ILobbyInitialBindSource, ILobbyFinalBindSource> { }
    public abstract class LobbyBindTarget : BindTarget<ILobbyInitialBindSource, ILobbyFinalBindSource>, ILobbyBindTarget { }

    public class LobbyBinder : Binder<ILobbyInitialBindSource, ILobbyFinalBindSource, ILobbyBindTarget>
    {
        public LobbyBinder(ILobbyInitialBindSource preBindSource) : base(preBindSource) { }
    }


}
