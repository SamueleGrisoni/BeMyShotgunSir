namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDrivingState
    {
        void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed);
        void CheckStateChange(IDrivingStateContext controller, ReplicateData data, bool isReplayed);
        void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed);
        void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed);
    }
}
