using BeMyShotgunSir.Scripts.Utils;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class IdleDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data) =>
            Log.DLazy(() => $"Enter idle state.", this, controller.Log);
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data)
        {
            if (data.IsStarting)
            {
                Log.DLazy(() => "Player has clicked start button", this, controller.Log);
                controller.ChangeState(controller.NormalState, data);
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data) =>
            controller.ApplyGravity(controller.NormalStats.Gravity);

        public void Exit(IDrivingStateContext controller, ReplicateData data) =>
            Log.DLazy(() => "Exiting idle state", this, controller.Log);
    }
}
