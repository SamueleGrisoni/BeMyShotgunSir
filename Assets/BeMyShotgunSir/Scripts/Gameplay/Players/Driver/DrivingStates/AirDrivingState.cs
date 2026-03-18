
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class AirDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller) { }
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            if (controller.IsGrounded)
            {
                controller.ChangeState(controller.NormalState);
            }
        }
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
            //controller.ApplyAcceleration(controller.ParentTransform.forward);
            controller.ApplyGravity(20f);
        }
        public void Exit(IDriverControllerContext controller) { }
    }
}
