using UnityEngine;
namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver.DrivingStates
{
    public class IdleDrivingState : IDrivingState
    {
        public void Enter(IDrivingStateContext controller, ReplicateData data) { }
        public void CheckStateChange(IDrivingStateContext controller, ReplicateData data)
        {
            if (data.IsStarting)
            {
                Debug.Log("Player has clicked start button");
                controller.ChangeState(controller.NormalState, data);
            }
        }
        public void RunInputs(IDrivingStateContext controller, ReplicateData data)
        {
            controller.ApplyGravity(controller.NormalStats.Gravity);
        }
        public void Exit(IDrivingStateContext controller, ReplicateData data) { }
    }
}