using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class IdleDrivingState : IDrivingState
    {
        public override void Enter(IDrivingState previousState, IDriverControllerContext controller, ReplicateData data)
        {
            _previousState = previousState;
            controller.SetMaxSpeed(0f);
        }
        public override void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if (data.IsStarting)
            {
                Debug.Log("Player has clicked start button");
                controller.ChangeState(controller.NormalState, data);
            }
        }
        public override void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyGravity(controller.NormalStats.Gravity);
        }
        public override void Exit(IDriverControllerContext controller, ReplicateData data) { }
    }
}