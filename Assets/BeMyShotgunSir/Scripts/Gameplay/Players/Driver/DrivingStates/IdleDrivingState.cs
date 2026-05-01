using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class IdleDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller, ReplicateData data) { }
        public void CheckStateChange(IDriverControllerContext controller, ReplicateData data)
        {
            if (data.IsStarting)
            {
                Debug.Log("Player has clicked start button");
                controller.ChangeState(controller.NormalState, data);
            }
        }
        public void RunInputs(IDriverControllerContext controller, ReplicateData data)
        {
            controller.ApplyGravity(controller.NormalStats.Gravity);
        }
        public void Exit(IDriverControllerContext controller, ReplicateData data) { }
    }
}