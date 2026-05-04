using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class GrassDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data)
        {
            controller.SetMaxSpeed(controller.GrassStats.MaxSpeed);
            Debug.Log($"Enter grass state. I am arriving from {controller.PreviousDrivingState.GetType().Name}");
        }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType != GroundType.Grass)
            {
                controller.ChangeState(controller.PreviousDrivingState, data);
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