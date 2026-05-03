using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Enter drift state");
            controller.SetMaxSpeed(controller.NormalStats.MaxSpeed);
            controller.DriftDirection = Mathf.Sign(data.SteerInput);

            //if (controller.IsOnwer || controller.IsServer) // TODO forse non è necessario
            controller.BatteryChargeTimer = 0f;
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if (controller.IsOnGrass())
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
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
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
        public void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit drift state");
        }
    }
}
