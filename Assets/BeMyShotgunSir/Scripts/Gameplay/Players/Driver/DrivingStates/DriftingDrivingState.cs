using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller) { }
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            if (!controller.IsGrounded)
            {
                controller.ChangeState(controller.AirState);
            }

            if (!controller.IsDriftingButtonPressed)
            {
                controller.ChangeState(controller.NormalState);
            }
        }
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
            controller.ApplyAcceleration(controller.ParentTransform.forward);
            controller.ApplyGravity(controller.Stats.Gravity);

            float steerControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0f, 2f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput))
                : Mathf.Lerp(2f, 0f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput));
            controller.ApplySteering(steerControl * controller.DriftDirection);

            float driftControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0.5f, 2f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput))
                : Mathf.Lerp(2f, 0.5f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput));
            controller.AnimateSidecar(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.Stats.SteerAngularRotation, 0));
        }
        public void Exit(IDriverControllerContext controller) { }
    }
}
