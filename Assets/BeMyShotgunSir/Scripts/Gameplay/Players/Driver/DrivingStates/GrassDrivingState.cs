using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class GrassDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            controller.SetMaxSpeed(controller.GrassStats.MaxSpeed);
            Debug.Log("Enter grass state");
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if (!controller.IsOnGrass())
            {
                controller.ChangeState(controller.NormalState, data);
                return;
            }
        }
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.GrassStats.AccelerationForce);
            controller.ApplySteering(data.SteerInput, controller.GrassStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.GrassStats.SteerAngularRotation, 0), controller.GrassStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.GrassStats.LateralGripFactor);
            controller.ApplyGravity(controller.GrassStats.Gravity);
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit grass state");
        }
    }
}