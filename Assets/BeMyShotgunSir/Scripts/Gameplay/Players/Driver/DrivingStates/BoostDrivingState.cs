using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class BoostDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log($"Enter boost state. I am arriving from {controller.PreviousDrivingState.GetType().Name}");
            if (controller.CurrentBatteryCharge <= 0)
            {
                controller.ChangeState(controller.NormalState, data);
            }
            controller.SetMaxSpeed(controller.BoostStats.MaxSpeed);
            controller.BoostTimer = 0f;
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType == GroundType.Grass)
            {
                controller.ChangeState(controller.GrassState, data);
                return;
            }
            if (controller.IsOnwer || controller.IsServer)
            {
                controller.BoostTimer += controller.TickDelta();
                if (controller.BoostTimer >= controller.BatteryStats.ConsumeBatteryTimeRate)
                {
                    controller.CurrentBatteryCharge -= controller.BatteryStats.ConsumeBatteryAmountRate;
                    controller.BoostTimer = 0;
                }

                if (controller.CurrentBatteryCharge <= 0)
                {
                    controller.CurrentBatteryCharge = 0;
                    controller.ChangeState(controller.NormalState, data);
                    return;
                }
            }
            if (data.IsDrifting && data.SteerInput != 0)
            {
                controller.ChangeState(controller.DriftingState, data);
            }
            Debug.Log($"Current battery charge: {controller.CurrentBatteryCharge}");
        }
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.BoostStats.AccelerationForce);
            controller.ApplySteering(data.SteerInput, controller.BoostStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.BoostStats.SteerAngularRotation, 0), controller.BoostStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.BoostStats.LateralGripFactor);
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit boost state");
        }
    }
}