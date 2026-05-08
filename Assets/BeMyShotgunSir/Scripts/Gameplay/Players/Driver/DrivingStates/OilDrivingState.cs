namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class OilDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data)
        {
            controller.OilAnimationTimer = 0f;
            controller.IsOilAnimationActive = true;
        }
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data)
        {
            controller.OilAnimationTimer += controller.TickDelta();
            if (controller.OilAnimationTimer > controller.AnimationStats.OilAnimationDuration && controller.IsServer) // TODO togliere da qua velore hardcodato
            {
                controller.ChangeState(controller.PreviousDrivingState, data);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, 0f, controller.NormalStats.MaxSpeed);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data)
        {
            controller.IsOilAnimationActive = true;
        }
    }
}