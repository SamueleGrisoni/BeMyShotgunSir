namespace BeMyShotgunSir.Scripts.Gameplay.Players.Driver
{
    public interface IDrivingState
    {
        void Enter(IDriverControllerContext controller);
        void ExecuteUpdate(IDriverControllerContext controller);
        void ExecuteFixedUpdate(IDriverControllerContext controller);
        void Exit(IDriverControllerContext controller);
    }
}
