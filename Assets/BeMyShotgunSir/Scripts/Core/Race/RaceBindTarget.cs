namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceBindTarget : IBindTarget<RaceCommand, IRaceDataView> { }
    public abstract class RaceBindTarget : BindTarget<RaceCommand, IRaceDataView>, IRaceBindTarget { }
}
