using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOSteal_PU", menuName = "Be My Shotgun, Sir!/PowerUp/Steal", order = 0)]
    public class SOSteal_PU : SOPowerUp
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);

            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isStealPowerUpActive = true;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Ticking;
            powerUpsNetController.AddActivePowerUp(runtime);
            base.OnUse(runtime, context);
        }

        public override void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context)
        {
            base.OnChangeTarget(runtime, instanceId, targetTeamId, context);

            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);

            int ownerTeamId = runtime.ActivePowerUpData.OwnerTeamId;
            PowerUp targetSelectedPowerUp = raceNetStateStore.TryGetInventorySelectedSlot(targetTeamId, out PuSlot selectedSlot) ? selectedSlot.PowerUp : PowerUp.None;
            if (targetSelectedPowerUp != PowerUp.None)
            {
                if (raceNetStateStore.TryGetTeamInventory(targetTeamId, out InventoryData targetInventoryData))
                    raceNetStateStore.SetTeamInventory(targetTeamId, targetInventoryData.RemoveAndUpdateSelected());
                if (raceNetStateStore.TryGetTeamData(ownerTeamId, out RaceTeamData teamData))
                {
                    powerUpsNetController.AddPowerUpToTeam(ownerTeamId, targetSelectedPowerUp);
                }
            }
            runtime.Definition.OnExpire(runtime, context);
        }

        public override void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isStealPowerUpActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            base.OnExpire(runtime, context);
        }
    }
}
