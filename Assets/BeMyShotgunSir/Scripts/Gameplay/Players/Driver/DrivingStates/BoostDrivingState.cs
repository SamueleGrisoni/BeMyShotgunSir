using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class BoostDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data)
        {
            Debug.Log($"Enter boost state. I am arriving from {controller.PreviousDrivingState.GetType().Name}");
            if (controller.CurrentBatteryCharge <= 0)
            {
                controller.ChangeState(controller.NormalState, data);
            }
            controller.BoostTimer = 0f;
        }
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType == GroundType.Grass)
            {
                controller.ChangeState(controller.GrassState, data);
                return;
            }
            else if (groundType == GroundType.Oil)
            {
                controller.ChangeState(controller.OilState, data);
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
            //Debug.Log($"Current battery charge: {controller.CurrentBatteryCharge}");
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.BoostStats.AccelerationForce, controller.BoostStats.MaxSpeed);
            controller.ApplySteering(data.SteerInput, controller.BoostStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.BoostStats.SteerAngularRotation, 0), controller.BoostStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.BoostStats.LateralGripFactor);
            controller.ApplyGravity(controller.BoostStats.Gravity);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data)
        {
            Debug.Log("Exit boost state");
        }
    }
}