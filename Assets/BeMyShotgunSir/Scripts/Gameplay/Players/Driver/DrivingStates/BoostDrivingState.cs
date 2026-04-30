using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class BoostDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Enter boost state");
            if (controller.CurrentBatteryCharge <= 0)
            {
                controller.ChangeState(controller.NormalState, data);
            }
            controller.SetMaxSpeed(controller.Stats.MaxSpeedWithBoost);
            controller.BoostTimer = 0f;
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if (controller.IsOnwer || controller.IsServer)
            {
                controller.BoostTimer += controller.TickDelta();
                if (controller.BoostTimer >= controller.Stats.ConsumeBatteryTimeRate)
                {
                    controller.CurrentBatteryCharge -= controller.Stats.ConsumeBatteryAmountRate;
                    controller.BoostTimer = 0;
                }

                if (controller.CurrentBatteryCharge <= 0)
                {
                    controller.CurrentBatteryCharge = 0;
                    controller.ChangeState(controller.NormalState, data);
                    return;
                }
            }
            if (data.IsDrifting)
            {
                controller.ChangeState(controller.DriftingState, data);
            }
            Debug.Log($"Current battery charge: {controller.CurrentBatteryCharge}");
        }
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward);
            controller.ApplySteering(data.SteerInput);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.Stats.SteerAngularRotation, 0));
            controller.ApplyLateralGrip();
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit boost state");
        }
    }
}