
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;

namespace BeMyShotgunSir.Scripts.Gameplay.Player.Driver.DriftingStates
{
    public class IdleDrivingState : IDrivingState
    {
        public void Enter(IDriverControllerContext controller) { }
        public void ExecuteUpdate(IDriverControllerContext controller)
        {
            if (controller.IsStartButtonPressed)
            {
                controller.ChangeState(controller.NormalState);
            }
        }
        public void ExecuteFixedUpdate(IDriverControllerContext controller)
        {
        }
        public void Exit(IDriverControllerContext controller) { }
    }
}