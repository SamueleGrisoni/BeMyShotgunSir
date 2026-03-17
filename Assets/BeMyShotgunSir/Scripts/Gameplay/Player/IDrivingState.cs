namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public interface IDrivingState
    {
        void Enter(IDriverControllerContext controller);
        void ExecuteUpdate(IDriverControllerContext controller);
        void ExecuteFixedUpdate(IDriverControllerContext controller);
        void Exit(IDriverControllerContext controller);
    }
}