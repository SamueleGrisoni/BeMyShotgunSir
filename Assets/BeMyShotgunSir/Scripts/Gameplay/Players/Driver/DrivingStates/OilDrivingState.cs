using BeMyShotgunSir.Scripts.Utils;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class OilDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            Log.DLazy(() => "Entering oil state", this, controller.Log);
            controller.OilAnimationTimer = 0f;
            controller.IsOilAnimationActive = true;
            if (!isReplayed)
                controller.OilAnimation();
        }
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.OilAnimationTimer += controller.TickDelta();
            if (controller.OilAnimationTimer > controller.AnimationStats.OilAnimationDuration) // TODO togliere da qua velore hardcodato
            {
                controller.ChangeState(controller.PreviousDrivingState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyAcceleration(controller.SidecarForward, 0f, controller.NormalStats.MaxSpeed);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            Log.DLazy(() => "Exiting oil state", this, controller.Log);
            controller.IsOilAnimationActive = false;
        }
    }
}
