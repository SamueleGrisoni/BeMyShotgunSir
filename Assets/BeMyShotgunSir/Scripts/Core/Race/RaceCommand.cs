using BeMyShotgunSir.Scripts.Gameplay.PowerUps;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public class RaceCommand : ICommand
    {
        private RaceNetController _netController;
        private PowerUpsNetController _powerUpsNetController;
        public RaceCommand(RaceNetController netController, PowerUpsNetController powerUpsNetController)
        {
            _netController = netController;
            _powerUpsNetController = powerUpsNetController;
        }
        public void LeaveRace_CMRequest() { /*TODO*/ }
        public void EquipPowerUp(int slotIndex) => _powerUpsNetController.EquipPowerUp_ServerRpc(slotIndex);
        public void ActivatePowerUp() => _powerUpsNetController.ActivatePowerUp_ServerRpc();
        public void TriggerPowerUpAction(PowerUpActionType actionType) => _powerUpsNetController.TriggerAction_ServerRpc(actionType);
    }
}
