namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDrivingState
    {
        void Enter(IDrivingStateContext controller, ReplicateData data);
        void CheckStateChange(IDrivingStateContext controller, ReplicateData data);
        void RunInputs(IDrivingStateContext controller, ReplicateData data);
        void Exit(IDrivingStateContext controller, ReplicateData data);
    }
}
