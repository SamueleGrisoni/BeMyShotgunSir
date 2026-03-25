using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Player.Driver.DriftingStates
{

    public class BoostDrivingState : IDrivingState
    {
        private float _timer;
        public void Enter(IDriverControllerContext controller)
        {
            if (controller.CurrentBatteryCharge <= 0)
            {
                controller.ChangeState(controller.NormalState);
            }
            controller.SetMaxSpeed(controller.Stats.MaxSpeedWithBoost);
            _timer = 0f;
        }
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            _timer += Time.deltaTime;
            if (_timer >= controller.Stats.ConsumeBatteryTimeRate)
            {
                controller.CurrentBatteryCharge -= controller.Stats.ConsumeBatteryAmountRate;
                _timer = 0;
            }

            if (controller.CurrentBatteryCharge <= 0)
            {
                controller.CurrentBatteryCharge = 0;
                controller.ChangeState(controller.NormalState);
            }

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
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
            controller.ApplyAcceleration(controller.SidecarTransform.forward);
            controller.ApplyGravity(controller.Stats.Gravity);
            controller.ApplySteering(controller.SteerInput);
            controller.ApplyLateralGrip(controller.SidecarTransform.forward);
            controller.AnimateSidecar(Quaternion.Euler(0, controller.SteerInput * controller.Stats.SteerAngularRotation, 0));
        }
        public void Exit(IDriverControllerContext controller)
        {
        }

    }
}