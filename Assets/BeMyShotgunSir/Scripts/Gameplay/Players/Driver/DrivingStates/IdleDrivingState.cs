using BeMyShotgunSir.Scripts.Utils;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class IdleDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => $"Enter idle state.", this, controller.Log);
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            if (data.IsStarting)
            {
                controller.ChangeState(controller.NormalState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => controller.ApplyAcceleration(controller.SidecarForward, 0f, 0f);
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => "Exiting idle state", this, controller.Log);
    }
}
