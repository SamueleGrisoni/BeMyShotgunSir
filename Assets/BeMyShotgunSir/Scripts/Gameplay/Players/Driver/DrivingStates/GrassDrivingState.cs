using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class GrassDrivingState : IDrivingState
    {
        public override void Enter(IDrivingState previousState, IDriverControllerContext controller, ReplicateData data)
        {
            _previousState = previousState;
            controller.SetMaxSpeed(controller.GrassStats.MaxSpeed);
            Debug.Log($"Enter grass state. I am arriving from {_previousState.GetType().Name}");
        }
        public override void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            GroundType groundType = controller.CheckGround();
            if (groundType != GroundType.Grass)
            {
                // If you enter grass while you were in boost mode, you return to boost mode when you exit grass
                controller.ChangeState(_previousState, data);
                return;
            }
        }
        public override void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, controller.GrassStats.AccelerationForce);
            controller.ApplySteering(data.SteerInput, controller.GrassStats.SteeringForce);
            controller.ApplyVisualRotation(Quaternion.Euler(0, data.SteerInput * controller.GrassStats.SteerAngularRotation, 0), controller.GrassStats.SteerAngularRotationSlerp);
            controller.ApplyLateralGrip(controller.GrassStats.LateralGripFactor);
            controller.ApplyGravity(controller.GrassStats.Gravity);
        }
        public override void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit grass state");
        }
    }
}