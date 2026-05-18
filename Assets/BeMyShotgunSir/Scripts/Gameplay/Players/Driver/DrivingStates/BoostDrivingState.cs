using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class BoostDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            Log.DLazy(() => $"Enter boost state. I am arriving from {controller.PreviousDrivingState.GetType().Name}", this, controller.Log);
            if (controller.CurrentBatteryCharge <= 0)
            {
                controller.ChangeState(controller.NormalState, data, isReplayed);
            }
            controller.BoostTimer = 0f;
        }
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType == GroundType.Grass)
            {
                controller.ChangeState(controller.GrassState, data, isReplayed);
                return;
            }
            else if (groundType == GroundType.Oil)
            {
                controller.ChangeState(controller.OilState, data, isReplayed);
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
                    controller.ChangeState(controller.NormalState, data, isReplayed);
                    return;
                }
            }
            if (data.IsDrifting && data.SteerInput != 0)
            {
                controller.ChangeState(controller.DriftingState, data, isReplayed);
            }
            if (controller.CheckForkBarrierCollision())
            {
                controller.ChangeState(controller.BumpState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.BoostStats.AccelerationForce, controller.BoostStats.MaxSpeed);
            controller.ApplySteering(data.SteerInput, controller.BoostStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.BoostStats.SteerAngularRotation, 0), controller.BoostStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.BoostStats.LateralGripFactor);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => "Exiting boost state", this, controller.Log);
    }
}
