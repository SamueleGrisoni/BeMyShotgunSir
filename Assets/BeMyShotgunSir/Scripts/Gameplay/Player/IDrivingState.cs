namespace BeMyShotgunSir.Scripts.Gameplay.Player
{
    public interface IDrivingState
    {
        void Enter(DriverController controller);
        void ExecuteUpdate(DriverController controller);
        void ExecuteFixedUpdate(DriverController controller);
        void Exit(DriverController controller);
    }
}