using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOArmor_PU", menuName = "Be My Shotgun, Sir!/PowerUp/Armor", order = 0)]
    public class SOArmor_PU : SOPowerUp
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isArmorActive = true;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            Log.DLazy(() => $"Team {runtime.ActivePowerUpData.OwnerTeamId} is using Armor.", this);
            powerUpsNetController.AddActivePowerUp(runtime);
        }
    }
}
