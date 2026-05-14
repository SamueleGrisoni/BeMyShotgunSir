using BeMyShotgunSir.Scripts.Core.Race;
using BeMyShotgunSir.Scripts.Utils;
using FishNet.Object;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Gameplay.PowerUps
{
    public interface IPowerUpStrategy
    {
        bool TryPreliminaryCheck(InfoUsePowerUp info, StrategyContext context);
        PowerUpRuntime OnCreateRuntime(InfoUsePowerUp info, StrategyContext context);
        bool CanActivate(PowerUpRuntime runtime, StrategyContext context);
        void OnActivate(PowerUpRuntime runtime, StrategyContext context);
        void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context);
        void OnUse(PowerUpRuntime runtime, StrategyContext context);
        void OnTick(float dt, PowerUpRuntime runtime, StrategyContext context);
        void OnAction(PowerUpRuntime runtime, StrategyContext context, PowerUpAction action);
        void OnExpire(PowerUpRuntime runtime, StrategyContext context);
        void OnDeactivate(PowerUpRuntime runtime, StrategyContext context);
    }

    [CreateAssetMenu(fileName = "SOPowerUps", menuName = "Be My Shotgun, Sir!/PowerUp", order = 0)]
    public abstract class SOPowerUp : ScriptableObject, IPowerUpStrategy
    {
        [field: SerializeField] public PowerUp PowerUpType { get; private set; }
        [field: SerializeField] public PowerUpClass PowerUpClass { get; private set; }
        [field: SerializeField] public bool IsOwnerTargeted { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }

        public virtual bool TryPreliminaryCheck(InfoUsePowerUp info, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            if (!raceNetStateStore.TryGetTeamInventory(info.OwnerTeamId, out InventoryData inventory))
            {
                Log.WLazy(() => $"Trying to activate power-up for team {info.OwnerTeamId} but no inventory found.", this);
                return false;
            }
            if (!inventory.SelectedSlot.HasValue || inventory.SelectedSlot.Value.PowerUp == PowerUp.None)
            {
                Log.WLazy(() => $"Trying to activate power-up for team {info.OwnerTeamId} but no slot selected.", this);
                return false;
            }
            if (inventory.SelectedSlot.Value.PowerUp != info.PowerUp)
            {
                Log.WLazy(() => $"Trying to activate power-up {info.PowerUp} for team {info.OwnerTeamId} but selected slot contains {inventory.SelectedSlot}.", this);
                return false;
            }
            return true;
        }

        public virtual PowerUpRuntime OnCreateRuntime(InfoUsePowerUp info, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            var activePowerUpData = new ActivePowerUp(info, powerUpsNetController.GetInstanceId(), this);

            NetworkObject targetNob = null;
            if (IsOwnerTargeted)
                //for owner-targeted power-ups the target nob is the owner's team itself
                raceNetStateStore.TryGetTeamNob(info.OwnerTeamId, out targetNob);
            return new PowerUpRuntime(this, activePowerUpData, targetNob);
        }

        public virtual bool CanActivate(PowerUpRuntime runtime, StrategyContext context) => true;

        public virtual void OnActivate(PowerUpRuntime runtime, StrategyContext context)
        {
            if (runtime.Definition.IsOwnerTargeted)
            {
                UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
                if (raceNetStateStore.TryGetTeamNob(runtime.ActivePowerUpData.OwnerTeamId, out NetworkObject ownerNob))
                    runtime.TargetNob = ownerNob;
                else
                    Log.WLazy(() => $"Trying to activate power-up {runtime.Definition.PowerUpType} for team {runtime.ActivePowerUpData.OwnerTeamId} but no team nob found.", this);
                runtime.ActivePowerUpData.TargetTeamId = runtime.ActivePowerUpData.OwnerTeamId;
            }
            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Active;
            if (runtime.Definition.PowerUpClass == PowerUpClass.TimeBased || runtime.Definition.PowerUpClass == PowerUpClass.OneShot)
                runtime.Definition.OnUse(runtime, context);
        }

        public virtual void OnChangeTarget(PowerUpRuntime runtime, int instanceId, int targetTeamId, StrategyContext context)
        {
            if (context.RaceNetStateStore.TryGetTeamNob(targetTeamId, out NetworkObject targetNob))
            {
                runtime.TargetNob = targetNob;
                runtime.ActivePowerUpData.TargetTeamId = targetTeamId;
                // runtime.Definition.OnUse(runtime, context);
                Log.DLazy(() => $"Changed target of power-up instance {instanceId} to team {targetTeamId}.", this);
            }
        }
        public virtual void OnUse(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);

            raceNetStateStore.TryGetTeamInventory(runtime.ActivePowerUpData.OwnerTeamId, out InventoryData inventory);
            raceNetStateStore.SetTeamInventory(runtime.ActivePowerUpData.OwnerTeamId, inventory.RemoveAndUpdateSelected());
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData = teamData.AddActivePowerUpResult(new PowerUpIdentifier(runtime.Definition.PowerUpType, runtime.ActivePowerUpData.ManagerInstanceId));
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);

            Log.DLazy(() => $"Using power-up {runtime.Definition.PowerUpType} instance {runtime.ActivePowerUpData.ManagerInstanceId} of team {runtime.ActivePowerUpData.OwnerTeamId}.", this);
        }
        public virtual void OnTick(float dt, PowerUpRuntime runtime, StrategyContext context)
        {
            if (runtime.ActivePowerUpData.PowerUpState == PowerUpState.Ticking)
            {
                runtime.ActivePowerUpData.RemainingDuration -= dt;
                if (runtime.ActivePowerUpData.RemainingDuration <= 0)
                    runtime.Definition.OnExpire(runtime, context);
            }
        }

        public virtual void OnAction(PowerUpRuntime runtime, StrategyContext context, PowerUpAction action) { }

        public virtual void OnDeactivate(PowerUpRuntime runtime, StrategyContext context) { }

        public virtual void OnExpire(PowerUpRuntime runtime, StrategyContext context)
        {
            UnwrapContext(context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController);
            runtime.ActivePowerUpData.PowerUpState = PowerUpState.Expired;
            powerUpsNetController.RemoveActivePowerUp(runtime.ActivePowerUpData.ManagerInstanceId);
            //TODO clean activePowerUp in team data?
            raceNetStateStore.TryGetTeamData(runtime.ActivePowerUpData.OwnerTeamId, out RaceTeamData teamData);
            teamData = teamData.RemoveActivePowerUpResult(runtime.ActivePowerUpData.ManagerInstanceId);
            raceNetStateStore.SetTeamData(runtime.ActivePowerUpData.OwnerTeamId, teamData);

            Log.DLazy(() => $"Power-up {runtime.Definition.PowerUpType} instance {runtime.ActivePowerUpData.ManagerInstanceId} of team {runtime.ActivePowerUpData.OwnerTeamId} has expired.", this);
        }

        public void UnwrapContext(StrategyContext context, out RaceNetStateStore raceNetStateStore, out PowerUpsNetController powerUpsNetController)
        {
            raceNetStateStore = context.RaceNetStateStore;
            powerUpsNetController = context.PowerUpsNetController;
        }
    }
}
