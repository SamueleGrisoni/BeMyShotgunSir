using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players.Driver;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SORoadBlock_PU", menuName = "Be My Shotgun, Sir!/PowerUp/RoadBlock", order = 0)]
    public class SORoadBlock_PU : SOPowerUp
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);

            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isRoadBlockActive = true;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);

            if (raceNetStateStore.TryGetDriverNob(runtime.ActivePowerUpData.OwnerTeamId,
                    out NetworkObject driverNob))
            {
                driverNob.GetComponent<DriverController>().ApplyRoadBlockEffect();
                Log.DLazy(() => $"Applying Road Block effect to driver of team {runtime.ActivePowerUpData.OwnerTeamId}.", this);
            }
            else
                Log.WLazy(() => $"Trying to apply Road Block effect for team {runtime.ActivePowerUpData.OwnerTeamId} but no driver nob found.", this);

            runtime.Definition.OnExpire(runtime, context);
            base.OnUse(runtime, context);
        }

        public override void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            //MEMO this is not strictly necessary for Road Block since its effect is applied instantly on use and then it expires
            //so never assigning isRoadBlockActive = true could be a solution. This is just to unify the logic
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController _);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isRoadBlockActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            base.OnExpire(runtime, context);
        }
    }
}
