using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data)
        {
            Log.DLazy(() => $"Enter drift state", this, controller.Log);
            controller.DriftDirection = Mathf.Sign(data.SteerInput);

            //if (controller.IsOnwer || controller.IsServer) // TODO forse non è necessario
            controller.BatteryChargeTimer = 0f;
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
                controller.ChangeState(controller.NormalState, data);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.ParentForward, controller.NormalStats.AccelerationForce, controller.NormalStats.MaxSpeed);

            float steerControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 0f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplySteering(steerControl * controller.DriftDirection, controller.NormalStats.SteeringForce);

            float driftControl = controller.DriftDirection == 1
                ? Mathf.Lerp(1f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 1f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplyVisualRotation(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.NormalStats.SteerAngularRotation, 0), controller.NormalStats.SteerAngularRotationSlerp);
            controller.ApplyGravity(controller.NormalStats.Gravity);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data) =>
            Log.DLazy(() => "Exiting drift state", this, controller.Log);
    }
}
