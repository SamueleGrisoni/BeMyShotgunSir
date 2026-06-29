using BeMyShotgunSir.Scripts.Core.Lobby;
using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Gameplay.Players;
using BeMyShotgunSir.Scripts.Gameplay.PowerUps;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    [CreateAssetMenu(fileName = "SOSpear_PU", menuName = "Be My Shotgun, Sir!/PowerUp/Spear", order = 0)]
    public class SOSpear_PU : SOPowerUp
    {
        public override void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);

            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isSpearPowerUpActive = true;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);

            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Ticking;
            powerUpsNetController.AddActivePowerUp(runtime);
            base.OnUse(runtime, context);
        }

        public override void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context)
        {
            base.OnChangeTarget(runtime, instanceId, targetTeamId, context);

            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isSpearPowerUpActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);

            int ownerTeamId = runtime.ActivePowerUpData.OwnerTeamId;
            NetworkObject targetShotgun = raceNetStateStore.TryGetShotgunNob(targetTeamId, out NetworkObject nob) ? nob : null;
            if (targetShotgun != null)
            {
                if (raceNetStateStore.LobbyNetStateStore.TryGetPlayerState(teamData.ShotgunConnectionId, out LobbyPlayerState targetPlayerState))
                {
                    if (targetPlayerState.Connection != null)
                    {
                        raceNetStateStore.TryGetTeamData(targetTeamId, out RaceTeamData targetTeamData);
                        targetTeamData.ActivePowerUpInfo.isTargetedBySpear = true;
                        raceNetStateStore.SetTeamData(targetTeamId, targetTeamData);
                        targetShotgun.GetComponent<ShotgunController>().ApplySpearEffect_TargetRpc(targetPlayerState.Connection);
                        targetShotgun.GetComponent<ShotgunController>().ApplySpearEffect_TargetRpc(raceNetStateStore.LobbyNetStateStore.PlayerStates[ownerTeamId].Connection);
                    }
                    else
                        Log.WLazy(() => $"Trying to apply Spear effect for team {targetTeamId} but no connection found for player.", this);
                }
            }
            else
                Log.WLazy(() => $"Trying to apply Spear effect for team {targetTeamId} but no shotgun nob found.", this);

            runtime.ActivePowerUpData.RemainingDuration = Duration;
        }

        public override void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData.ActivePowerUpInfo.isSpearPowerUpActive = false;
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);

            if (runtime.ActivePowerUpData.TargetTeamId.HasValue)
            {
                raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.TargetTeamId.Value, out RaceTeamData targetTeamData);
                targetTeamData.ActivePowerUpInfo.isTargetedBySpear = false;
                raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.TargetTeamId.Value, targetTeamData);
            }

            else Log.WLazy(() => $"Trying to expire Spear effect for team {runtime.ActivePowerUpData.OwnerTeamId} but no target team id found.", this);
            base.OnExpire(runtime, context);
        }
    }
}
