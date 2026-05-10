using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOInvisibility_PU", menuName = "Be My Shotgun, Sir!/PowerUp/Invisibility", order = 0)]
    public class SOInvisibility_PU : SOPowerUp //TODO
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isInvisibilityActive = true;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            if (raceNetStateStore.TryGetDriverNob(runtime.ActivePowerUpData.OwnerTeamId, out NetworkObject driverNob))
                // driverNob.GetComponent<DriverController>().ApplyInvisibilityEffect(); //TODO
                Log.DLazy(() => $"Applying Invisibility effect to driver of team {runtime.ActivePowerUpData.OwnerTeamId}.", this);
            else
                Log.WLazy(() => $"Trying to apply Invisibility effect for team {runtime.ActivePowerUpData.OwnerTeamId} but no driver nob found.", this);

            Log.DLazy(() => $"Team {runtime.ActivePowerUpData.OwnerTeamId} is using Invisibility.", this);
            powerUpsNetController.AddActivePowerUp(runtime);
        }

        public override void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isInvisibilityActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            base.OnExpire(runtime, context);
        }
    }
}
