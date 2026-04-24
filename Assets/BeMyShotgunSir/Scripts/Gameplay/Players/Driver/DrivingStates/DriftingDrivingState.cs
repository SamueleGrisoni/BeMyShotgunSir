using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        private float _timer;
        public void Enter(IDriverControllerContext controller)
        {
            controller.SetMaxSpeed(controller.Stats.MaxSpeed);
            _timer = 0f;
        }
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            _timer += Time.deltaTime;
            if (_timer >= controller.Stats.ChargeBatteryTimeRate)
            {
                controller.CurrentBatteryCharge += controller.Stats.ChargeBatterAmountRate;
                _timer = 0;
            }
            if (!controller.IsGrounded)
            {
                controller.ChangeState(controller.AirState);
                return;
            }

            if (!controller.IsDriftingButtonPressed)
            {
                controller.ChangeState(controller.NormalState);
                return;
            }

        }
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
            controller.ApplyAcceleration(controller.ParentForward);

            float steerControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0f, 2f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput))
                : Mathf.Lerp(2f, 0f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput));
            controller.ApplySteering(steerControl * controller.DriftDirection);

            float driftControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0.5f, 2f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput))
                : Mathf.Lerp(2f, 0.5f, Mathf.InverseLerp(-1f, 1f, controller.SteerInput));
            controller.ApplyVisualRotation(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.Stats.SteerAngularRotation, 0));
            controller.ApplyGravity(controller.Stats.Gravity);
            //controller.AnimateSidecar(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.Stats.SteerAngularRotation, 0));
        }
        public void Exit(IDriverControllerContext controller) { }
    }
}
