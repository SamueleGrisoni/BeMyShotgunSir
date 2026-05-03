using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        public override void Enter(IDrivingState previousState, IDriverControllerContext controller, ReplicateData data)
        {
            _previousState = previousState;
            controller.SetMaxSpeed(controller.NormalStats.MaxSpeed);
            controller.DriftDirection = Mathf.Sign(data.SteerInput);

            //if (controller.IsOnwer || controller.IsServer) // TODO forse non è necessario
            controller.BatteryChargeTimer = 0f;
            Debug.Log($"Enter drifting state. I am arriving from {_previousState.GetType().Name}");
        }
        public override void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType == GroundType.Grass)
            {
                controller.ChangeState(controller.GrassState, data);
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
        public override void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.ParentForward, controller.NormalStats.AccelerationForce);

            float steerControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 0f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplySteering(steerControl * controller.DriftDirection, controller.NormalStats.SteeringForce);

            float driftControl = controller.DriftDirection == 1
                ? Mathf.Lerp(1f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 1f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplyVisualRotation(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.NormalStats.SteerAngularRotation, 0), controller.NormalStats.SteerAngularRotationSlerp);
        }
        public override void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit drift state");
        }
    }
}
