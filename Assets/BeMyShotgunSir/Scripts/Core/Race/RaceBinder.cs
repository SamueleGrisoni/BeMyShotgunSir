namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceInitialBindSource : IBindSource
    {
        RaceCommand Command { get; }
        RaceViewModel ViewModel { get; }
    }
    public interface IRaceFinalBindSource : IBindSource { }
    public interface IRaceBindSources : IRaceInitialBindSource, IRaceFinalBindSource, IBindSources { }
    public interface IRaceBindTarget : IBindTarget<IRaceInitialBindSource, IRaceFinalBindSource> { }
    public abstract class RaceBindTarget : BindTarget<IRaceInitialBindSource, IRaceFinalBindSource>, IRaceBindTarget { }

    public class RaceBinder : Binder<IRaceInitialBindSource, IRaceFinalBindSource, IRaceBindTarget>
    {
        public RaceBinder(IRaceInitialBindSource source, IRaceFinalBindSource bindSource) : base(source, bindSource) { }
    }


}
