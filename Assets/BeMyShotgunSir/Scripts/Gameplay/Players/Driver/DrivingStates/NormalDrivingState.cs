using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class NormalDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller) => controller.SetMaxSpeed(controller.Stats.MaxSpeed);
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            if (!controller.IsGrounded)
            {
                controller.ChangeState(controller.AirState);
                return;
            }

            if (controller.IsDriftingButtonPressed)
            {
                controller.DriftDirection = Mathf.Sign(controller.SteerInput);
                controller.ChangeState(controller.DriftingState);
                return;
            }

            if (controller.IsBoostButtonPressed)
            {
                controller.ChangeState(controller.BoostState);
                return;
            }
        }
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
            controller.ApplyAcceleration(controller.SidecarForward);
            controller.ApplySteering(controller.SteerInput);
            controller.ApplyLateralGrip();
            controller.ApplyVisualRotation(Quaternion.Euler(0, controller.SteerInput * controller.Stats.SteerAngularRotation, 0));
            controller.ApplyGravity(controller.Stats.Gravity);
            //controller.AnimateSidecar(Quaternion.Euler(0, controller.SteerInput * controller.Stats.SteerAngularRotation, 0));
        }
        public void Exit(IDriverControllerContext controller) { }
    }
}
