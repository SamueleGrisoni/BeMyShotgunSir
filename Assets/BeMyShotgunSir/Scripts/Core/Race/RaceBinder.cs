using BeMyShotgunSir.Scripts.Gameplay.Track;
using BeMyShotgunSir.Scripts.UI;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceInitialBindSource : IBindSource
    {
        RaceCommand Command { get; }
        RaceViewModel ViewModel { get; }
    }
    public interface IRaceFinalBindSource : IBindSource
    {
        IRoadManager RoadManager { get; }
        IInputPublisher InputPublisher { get; }
    }
    public interface IRaceBindSources : IRaceInitialBindSource, IRaceFinalBindSource, IBindSources { }
    public interface IRaceBindTarget : IBindTarget<IRaceInitialBindSource, IRaceFinalBindSource> { }
    public abstract class RaceBindTarget : BindTarget<IRaceInitialBindSource, IRaceFinalBindSource>, IRaceBindTarget { }

    public class RaceBinder : Binder<IRaceInitialBindSource, IRaceFinalBindSource, IRaceBindTarget>
    {
        public RaceBinder(IRaceInitialBindSource source) : base(source) { }
    }


}
