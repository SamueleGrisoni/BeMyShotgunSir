namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class OilDrivingState : IDrivingState
    {
        private float _timer;
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            controller.OilAnimationTimer = 0f;
            controller.IsOilAnimationActive = true;
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            controller.OilAnimationTimer += controller.TickDelta();
            if (controller.OilAnimationTimer > 1f && controller.IsServer) // TODO togliere da qua velore hardcodato
            {
                controller.ChangeState(controller.PreviousDrivingState, data);
                return;
            }
        }
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, 0f);
            controller.OilAnimation();
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            controller.IsOilAnimationActive = true;
        }
    }
}