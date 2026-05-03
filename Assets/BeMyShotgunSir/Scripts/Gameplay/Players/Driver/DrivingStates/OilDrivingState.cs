using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class OilDrivingState : IDrivingState
    {
        private Quaternion _lastSidecarLocalRotation;
        private float _timer;
        public override void Enter(IDrivingState previousState, IDriverControllerContext controller, ReplicateData data)
        {
            _previousState = previousState;
            _lastSidecarLocalRotation = controller.SidecarLocalRotation;
            _timer = 0;
            Debug.Log($"Enter oil state. I am arriving from {_previousState.GetType().Name}");
        }
        public override void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            _timer += controller.TickDelta();

            if (_timer > controller.BatteryStats.OilAnimationDuration)
            {
                controller.ChangeState(_previousState, data);
                return;
            }
        }
        public override void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyAcceleration(controller.SidecarForward, 0f);
            /*
            controller.ApplyVisualRotation(
                Quaternion.Euler(0, controller.BatteryStats.OilSpeedAnimaton * controller.TickDelta(), 0),
                45f
            );
            */
            float angleThisTick = (controller.BatteryStats.OilTotalRotation / controller.BatteryStats.OilAnimationDuration) * controller.TickDelta();
            controller.SidecarLocalRotation *= Quaternion.Euler(0, angleThisTick, 0);
        }
        public override void Exit(IDriverControllerContext controller, ReplicateData data)
        {
            Debug.Log("Exit oil state");
        }

    }
}