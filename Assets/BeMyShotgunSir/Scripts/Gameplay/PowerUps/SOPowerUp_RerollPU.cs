using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOReroll_PU", menuName = "Be My Shotgun, Sir!/PowerUp/Reroll", order = 0)]
    public class SOReroll_PU : SOPowerUp
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isRerollPowerUpActive = true;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            if (raceNetStateStore.TryGetDriverNob(runtime.ActivePowerUpData.OwnerTeamId, out NetworkObject driverNob))
                // driverNob.GetComponent<DriverController>().ApplyRerollEffect(); //TODO
                Log.DLazy(() => $"Applying Reroll effect to driver of team {runtime.ActivePowerUpData.OwnerTeamId}.", this);
            else
                Log.WLazy(() => $"Trying to apply Reroll effect for team {runtime.ActivePowerUpData.OwnerTeamId} but no driver nob found.", this);

            Log.DLazy(() => $"Team {runtime.ActivePowerUpData.OwnerTeamId} is using Reroll.", this);
            OnExpire(runtime, context);
        }

        public override void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context)
        {
            base.OnChangeTarget(runtime, instanceId, targetTeamId, context);
            //TODO send rpc to new target
            runtime.Definition.OnExpire(runtime, context);
        }

        public override void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isRerollPowerUpActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            base.OnExpire(runtime, context);
        }
    }
}
