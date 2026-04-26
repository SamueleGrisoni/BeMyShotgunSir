using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class DriftingDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            controller.SetMaxSpeed(controller.Stats.MaxSpeed);
            controller.DriftDirection = Mathf.Sign(data.SteerInput);

            if (controller.IsOnwer || controller.IsServer) // TODO forse non è necessario
                controller.BatteryChargeTimer = 0f;
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if ((controller.IsOnwer || controller.IsServer) && controller.CurrentBatteryCharge < controller.Stats.MaxBatteryCharge)
            {
                controller.BatteryChargeTimer += controller.TickDelta();
                if (controller.BatteryChargeTimer >= controller.Stats.ChargeBatteryTimeRate)
                {
                    controller.CurrentBatteryCharge += controller.Stats.ChargeBatterAmountRate;
                    controller.BatteryChargeTimer = 0f;
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
            controller.ApplyAcceleration(controller.ParentForward);

            float steerControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 0f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplySteering(steerControl * controller.DriftDirection);

            float driftControl = controller.DriftDirection == 1
                ? Mathf.Lerp(0.5f, 2f, Mathf.InverseLerp(-1f, 1f, data.SteerInput))
                : Mathf.Lerp(2f, 0.5f, Mathf.InverseLerp(-1f, 1f, data.SteerInput));
            controller.ApplyVisualRotation(Quaternion.Euler(0, controller.DriftDirection * driftControl * controller.Stats.SteerAngularRotation, 0));
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data) { }
    }
}
