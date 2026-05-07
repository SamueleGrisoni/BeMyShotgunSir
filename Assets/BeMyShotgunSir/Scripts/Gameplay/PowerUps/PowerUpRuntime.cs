using FishNet.Object;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public class PowerUpRuntime
    {
        public SOPowerUp Definition;
        public ActivePowerUp ActivePowerUpData;
        public NetworkObject TargetNob;
        public PowerUpRuntime(SOPowerUp definition, ActivePowerUp activePowerUpData, NetworkObject targetNob)
        {
            Definition = definition;
            ActivePowerUpData = activePowerUpData;
            TargetNob = targetNob;
        }
    }
}
