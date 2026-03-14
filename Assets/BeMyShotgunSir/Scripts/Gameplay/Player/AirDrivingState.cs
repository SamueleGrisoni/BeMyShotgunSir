namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public class AirDrivingState : IDrivingState
    {
        public void Enter(DriverController controller) { }
        public void ExecuteUpdate(DriverController controller)
        {
            if (controller.IsGrounded)
            {
                controller.ChangeState(controller.NormalState);
            }
        }
        public void ExecuteFixedUpdate(DriverController controller)
        {
            controller.ApplyAcceleration(controller.ParentTransform.forward);
            controller.ApplyGravity(20f);
        }
        public void Exit(DriverController controller) { }
    }
}