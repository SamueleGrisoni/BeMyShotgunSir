namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDrivingState
    {
        void Enter(IDriverControllerContext controller, ReplicateData data);
        void CheckStateChange(IDriverControllerContext controller, ReplicateData data);
        void RunInputs(IDriverControllerContext controller, ReplicateData data);
        void Exit(IDriverControllerContext controller, ReplicateData data);
    }
}
