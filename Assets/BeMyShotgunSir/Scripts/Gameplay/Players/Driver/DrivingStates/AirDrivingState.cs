namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class AirDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller)
        {
        }
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            if (controller.IsGrounded)
            {
                controller.ChangeState(controller.NormalState);
                return;
            }
        }
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
            //controller.ApplyAcceleration(controller.ParentTransform.forward);
            //controller.ApplyGravity(20f);
            controller.ApplyGravity(controller.Stats.Gravity);
        }
        public void Exit(IDriverControllerContext controller) { }
    }
}
