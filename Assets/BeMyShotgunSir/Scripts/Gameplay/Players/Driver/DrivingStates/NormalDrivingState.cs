using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class NormalDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            controller.SetMaxSpeed(controller.NormalStats.MaxSpeed);
            //Debug.Log($"Enter normal state. I am arriving from {controller.PreviousDrivingState.GetType().Name}");
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
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
            if (data.IsDrifting && data.SteerInput != 0)
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
            controller.ApplyAcceleration(controller.SidecarForward, controller.NormalStats.AccelerationForce);
            controller.ApplySteering(data.SteerInput, controller.NormalStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.NormalStats.SteerAngularRotation, 0), controller.NormalStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.NormalStats.LateralGripFactor);
            controller.ApplyGravity(controller.NormalStats.Gravity);
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data)
        {
        }
    }
}
