using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
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
            if (raceNetStateStore.TryGetDriverNob(runtime.ActivePowerUpData.OwnerTeamId, out NetworkObject driverNob))
                // driverNob.GetComponent<DriverController>().ApplyStealEffect(); //TODO
                Log.DLazy(() => $"Applying Steal effect to driver of team {runtime.ActivePowerUpData.OwnerTeamId}.", this);
            else
                Log.WLazy(() => $"Trying to apply Steal effect for team {runtime.ActivePowerUpData.OwnerTeamId} but no driver nob found.", this);

            Log.DLazy(() => $"Team {runtime.ActivePowerUpData.OwnerTeamId} is using Steal.", this);
            powerUpsNetController.AddActivePowerUp(runtime);
            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Ticking;
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
            teamData.ActivePowerUpInfo.isStealPowerUpActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);
            base.OnExpire(runtime, context);
        }
    }
}
