namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceBinder : Binder<RaceCommand, IRaceDataView, IRaceBindTarget>
    {
        public RaceBinder(RaceCommand raceCommand, IRaceDataView raceDataView) : base(raceCommand, raceDataView) { }
    }
}
