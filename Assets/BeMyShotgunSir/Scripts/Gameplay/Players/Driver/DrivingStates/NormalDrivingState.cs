using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class NormalDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data) => controller.SetMaxSpeed(controller.Stats.MaxSpeed);
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if (data.IsDrifting)
            {
                controller.ChangeState(controller.DriftingState, data);
                return;
            }
            if (data.IsBoosting && controller.CurrentBatteryCharge > 0)
            {
                controller.ChangeState(controller.BoostState, data);
                return;
            }
        }
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward);
            controller.ApplySteering(data.SteerInput);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.Stats.SteerAngularRotation, 0));
            controller.ApplyLateralGrip();
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data) { }
    }
}
