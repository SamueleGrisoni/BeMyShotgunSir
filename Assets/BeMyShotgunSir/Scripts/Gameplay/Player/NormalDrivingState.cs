using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public class NormalDrivingState : IDrivingState
    {
        public void Enter(DriverController controller) { }
        public void ExecuteUpdate(DriverController controller)
        {
            if (!controller.IsGrounded)
            {
                controller.ChangeState(controller.AirState);
            }

            if (controller.IsDriftingButtonPressed)
            {
                controller.DriftDirection = Mathf.Sign(controller.SteerInput);
                controller.ChangeState(controller.DriftingState);
            }
        }
        public void ExecuteFixedUpdate(DriverController controller)
        {
            controller.ApplyAcceleration(controller.SidecarTransform.forward);
            controller.ApplyGravity(controller.Stats.Gravity);
            controller.ApplySteering(controller.SteerInput);
            controller.ApplyLateralGrip(controller.SidecarTransform.forward);
            controller.AnimateSidecar(Quaternion.Euler(0, controller.SteerInput * controller.Stats.SteerAngularRotation, 0));
        }
        public void Exit(DriverController controller) { }
    }
}