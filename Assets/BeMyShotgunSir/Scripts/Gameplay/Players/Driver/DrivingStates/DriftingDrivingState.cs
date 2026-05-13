using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.DriftDirection = data.DriftIntent;
            controller.BatteryChargeTimer = 0f;
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
            if (controller.CurrentBatteryCharge < controller.BatteryStats.MaxBatteryCharge)
            {
                controller.BatteryChargeTimer += controller.TickDelta();
                if (controller.BatteryChargeTimer >= controller.BatteryStats.ChargeBatteryTimeRate)
                {
                    controller.CurrentBatteryCharge += controller.BatteryStats.ChargeBatterAmountRate;

                    if (controller.CurrentBatteryCharge > controller.BatteryStats.MaxBatteryCharge)
                    {
                        controller.CurrentBatteryCharge = controller.BatteryStats.ChargeBatterAmountRate;
                    }
                }
            }
            if (!data.IsDrifting)
            {
                controller.ChangeState(controller.NormalState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyAcceleration(controller.ParentForward, controller.NormalStats.AccelerationForce, controller.NormalStats.MaxSpeed);

            float steerControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0f, 1.5f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(1.5f, 0f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplySteering(steerControl * controller.DriftDirection, controller.NormalStats.SteeringForce);

            float driftControl = controller.DriftDirection == 1
                ? Mathf.Lerp(1f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 1f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplyVisualRotation(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.NormalStats.SteerAngularRotation, 0), controller.NormalStats.SteerAngularRotationSlerp);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => "Exiting drift state", this, controller.Log);
    }
}
