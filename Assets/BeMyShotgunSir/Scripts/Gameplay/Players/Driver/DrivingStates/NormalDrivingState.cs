using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class NormalDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => $"Enter normal state", this, controller.Log);
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
            if (data.IsDrifting && data.SteerInput != 0)
            {
                controller.ChangeState(controller.DriftingState, data, isReplayed);
                return;
            }
            if (data.IsBoosting && controller.CurrentBatteryCharge > 0)
            {
                controller.ChangeState(controller.BoostState, data, isReplayed);
                return;
            }
            if (data.IsStarting)
            {
                controller.ChangeState(controller.IdleState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.NormalStats.AccelerationForce, controller.NormalStats.MaxSpeed);
            controller.ApplySteering(data.SteerInput, controller.NormalStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.NormalStats.SteerAngularRotation, 0), controller.NormalStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.NormalStats.LateralGripFactor);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => "Exiting normal state", this, controller.Log);
    }
}
