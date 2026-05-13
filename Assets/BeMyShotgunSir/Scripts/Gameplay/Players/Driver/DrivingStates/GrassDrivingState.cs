using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class GrassDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => $"Enter grass state", this, controller.Log);
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType != GroundType.Grass)
            {
                controller.ChangeState(controller.PreviousDrivingState, data, isReplayed);
                return;
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data, bool isReplayed)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.GrassStats.AccelerationForce, controller.GrassStats.MaxSpeed);
            controller.ApplySteering(data.SteerInput, controller.GrassStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.GrassStats.SteerAngularRotation, 0), controller.GrassStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.GrassStats.LateralGripFactor);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data, bool isReplayed) => Log.DLazy(() => "Exiting grass state", this, controller.Log);
    }
}
